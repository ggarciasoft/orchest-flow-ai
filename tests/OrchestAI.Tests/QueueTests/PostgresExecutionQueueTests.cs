using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrchestAI.Domain.Enums;
using OrchestAI.Infrastructure.Persistence;
using OrchestAI.Infrastructure.Queue;

namespace OrchestAI.Tests.QueueTests;

/// <summary>
/// Unit tests for <see cref="PostgresExecutionQueue"/> using the EF in-memory provider.
/// The SELECT FOR UPDATE SKIP LOCKED path requires a real PostgreSQL instance and is
/// covered by integration tests; this suite focuses on the EF-layer operations
/// (EnqueueAsync, MarkCompletedAsync, MarkFailedAsync) that are provider-agnostic.
/// </summary>
public sealed class PostgresExecutionQueueTests : IDisposable
{
    private readonly OrchestAIDbContext _db;
    private readonly PostgresExecutionQueue _queue;

    public PostgresExecutionQueueTests()
    {
        var opts = new DbContextOptionsBuilder<OrchestAIDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new OrchestAIDbContext(opts);
        _queue = new PostgresExecutionQueue(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task EnqueueAsync_ShouldPersistPendingItem()
    {
        var workflowId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await _queue.EnqueueAsync(workflowId, tenantId, "manual", "{}", CancellationToken.None);

        var saved = await _db.ExecutionQueue.SingleAsync();
        saved.WorkflowId.Should().Be(workflowId);
        saved.TenantId.Should().Be(tenantId);
        saved.TriggeredBy.Should().Be("manual");
        saved.Payload.Should().Be("{}");
        saved.Status.Should().Be(ExecutionQueueItemStatus.Pending);
        saved.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EnqueueAsync_MultipleTimes_ShouldPersistAllItems()
    {
        var tenantId = Guid.NewGuid();

        await _queue.EnqueueAsync(Guid.NewGuid(), tenantId, "manual", "{}", CancellationToken.None);
        await _queue.EnqueueAsync(Guid.NewGuid(), tenantId, "cron", "{}", CancellationToken.None);
        await _queue.EnqueueAsync(Guid.NewGuid(), tenantId, "webhook", "{}", CancellationToken.None);

        var count = await _db.ExecutionQueue.CountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task MarkCompletedAsync_ShouldSetDoneStatusAndCompletedAt()
    {
        var workflowId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await _queue.EnqueueAsync(workflowId, tenantId, "manual", "{}", CancellationToken.None);

        // Transition to Processing manually so MarkCompleted is valid
        var item = await _db.ExecutionQueue.SingleAsync();
        item.MarkProcessing();
        await _db.SaveChangesAsync();

        await _queue.MarkCompletedAsync(item.Id, CancellationToken.None);

        var updated = await _db.ExecutionQueue.FindAsync(item.Id);
        updated!.Status.Should().Be(ExecutionQueueItemStatus.Done);
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkFailedAsync_ShouldSetFailedStatusAndCompletedAt()
    {
        var workflowId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await _queue.EnqueueAsync(workflowId, tenantId, "cron", "{}", CancellationToken.None);

        // Transition to Processing manually so MarkFailed is valid
        var item = await _db.ExecutionQueue.SingleAsync();
        item.MarkProcessing();
        await _db.SaveChangesAsync();

        await _queue.MarkFailedAsync(item.Id, CancellationToken.None);

        var updated = await _db.ExecutionQueue.FindAsync(item.Id);
        updated!.Status.Should().Be(ExecutionQueueItemStatus.Failed);
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkCompletedAsync_WithUnknownId_ShouldNotThrow()
    {
        var act = async () => await _queue.MarkCompletedAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MarkFailedAsync_WithUnknownId_ShouldNotThrow()
    {
        var act = async () => await _queue.MarkFailedAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
