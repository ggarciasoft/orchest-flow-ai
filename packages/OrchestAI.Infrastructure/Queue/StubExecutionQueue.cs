using System.Collections.Concurrent;
using OrchestAI.Application.Abstractions;
using OrchestAI.Domain.Entities;
using OrchestAI.Domain.Enums;

namespace OrchestAI.Infrastructure.Queue;

/// <summary>
/// In-memory stub implementation of <see cref="IPersistentExecutionQueue"/>.
/// Uses a <see cref="ConcurrentQueue{T}"/> — data is lost on restart.
/// Suitable for tests and local development without a database.
/// </summary>
public sealed class StubExecutionQueue : IPersistentExecutionQueue
{
    private readonly ConcurrentQueue<ExecutionQueueItem> _queue = new();
    private readonly ConcurrentDictionary<Guid, ExecutionQueueItem> _index = new();

    /// <inheritdoc />
    public Task EnqueueAsync(Guid workflowId, Guid tenantId, string triggeredBy, string payload, CancellationToken ct = default)
    {
        var item = ExecutionQueueItem.Create(workflowId, tenantId, triggeredBy, payload);
        _index[item.Id] = item;
        _queue.Enqueue(item);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ExecutionQueueItem?> DequeueAsync(CancellationToken ct = default)
    {
        // Drain items whose status is no longer Pending (already claimed by a prior Dequeue call)
        while (_queue.TryPeek(out var head))
        {
            if (head.Status != ExecutionQueueItemStatus.Pending)
            {
                _queue.TryDequeue(out _);
                continue;
            }

            if (_queue.TryDequeue(out var item))
            {
                item.MarkProcessing();
                return Task.FromResult<ExecutionQueueItem?>(item);
            }
        }

        return Task.FromResult<ExecutionQueueItem?>(null);
    }

    /// <inheritdoc />
    public Task MarkCompletedAsync(Guid itemId, CancellationToken ct = default)
    {
        if (_index.TryGetValue(itemId, out var item))
            item.MarkCompleted();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MarkFailedAsync(Guid itemId, CancellationToken ct = default)
    {
        if (_index.TryGetValue(itemId, out var item))
            item.MarkFailed();
        return Task.CompletedTask;
    }
}
