using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Application.Abstractions;

/// <summary>
/// Persistent execution queue backed by a durable store (e.g. PostgreSQL).
/// Supports atomic dequeue with SELECT FOR UPDATE SKIP LOCKED semantics so
/// multiple worker instances can safely compete for items without duplication.
/// </summary>
public interface IPersistentExecutionQueue
{
    /// <summary>
    /// Adds a new item to the queue with <c>Pending</c> status.
    /// </summary>
    /// <param name="workflowId">The workflow to execute.</param>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="triggeredBy">Trigger source: "manual", "webhook", or "cron".</param>
    /// <param name="payload">JSON payload for the execution.</param>
    /// <param name="ct">Cancellation token.</param>
    Task EnqueueAsync(Guid workflowId, Guid tenantId, string triggeredBy, string payload, CancellationToken ct = default);

    /// <summary>
    /// Atomically dequeues one <c>Pending</c> item, transitions it to <c>Processing</c>,
    /// and returns it. Returns <c>null</c> when the queue is empty.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The claimed item, or <c>null</c> if no pending items exist.</returns>
    Task<ExecutionQueueItem?> DequeueAsync(CancellationToken ct = default);

    /// <summary>Marks a previously dequeued item as <c>Done</c>.</summary>
    /// <param name="itemId">The id of the item to mark completed.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkCompletedAsync(Guid itemId, CancellationToken ct = default);

    /// <summary>Marks a previously dequeued item as <c>Failed</c>.</summary>
    /// <param name="itemId">The id of the item to mark failed.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkFailedAsync(Guid itemId, CancellationToken ct = default);
}
