using Microsoft.EntityFrameworkCore;
using OrchestAI.Application.Abstractions;
using OrchestAI.Domain.Entities;
using OrchestAI.Domain.Enums;
using OrchestAI.Infrastructure.Persistence;

namespace OrchestAI.Infrastructure.Queue;

/// <summary>
/// PostgreSQL-backed persistent execution queue.
/// Uses SELECT FOR UPDATE SKIP LOCKED to atomically claim items so multiple
/// worker instances can safely consume the queue concurrently without duplication.
/// </summary>
public sealed class PostgresExecutionQueue : IPersistentExecutionQueue
{
    private readonly OrchestAIDbContext _db;

    /// <summary>Initialises the queue with the provided DbContext.</summary>
    public PostgresExecutionQueue(OrchestAIDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task EnqueueAsync(Guid workflowId, Guid tenantId, string triggeredBy, string payload, CancellationToken ct = default)
    {
        var item = ExecutionQueueItem.Create(workflowId, tenantId, triggeredBy, payload);
        _db.ExecutionQueue.Add(item);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses a raw SQL SKIP LOCKED query to atomically pick one pending row and
    /// transition it to Processing within a single transaction, preventing two
    /// workers from processing the same item.
    /// </remarks>
    public async Task<ExecutionQueueItem?> DequeueAsync(CancellationToken ct = default)
    {
        // Use raw SQL for SELECT FOR UPDATE SKIP LOCKED — not natively expressible via LINQ
        var item = await _db.ExecutionQueue
            .FromSqlRaw(
                """
                SELECT * FROM "ExecutionQueue"
                WHERE "Status" = 'Pending'
                ORDER BY "CreatedAt"
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return null;

        item.MarkProcessing();
        await _db.SaveChangesAsync(ct);
        return item;
    }

    /// <inheritdoc />
    public async Task MarkCompletedAsync(Guid itemId, CancellationToken ct = default)
    {
        var item = await _db.ExecutionQueue.FindAsync([itemId], ct);
        if (item is null) return;
        item.MarkCompleted();
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task MarkFailedAsync(Guid itemId, CancellationToken ct = default)
    {
        var item = await _db.ExecutionQueue.FindAsync([itemId], ct);
        if (item is null) return;
        item.MarkFailed();
        await _db.SaveChangesAsync(ct);
    }
}
