using OrchestAI.Domain.Enums;

namespace OrchestAI.Domain.Entities;

/// <summary>
/// Represents a persistent execution queue item stored in the database.
/// Acts as an outbox entry that workers atomically claim and process.
/// </summary>
public sealed class ExecutionQueueItem
{
    /// <summary>Gets the unique identifier for this queue item.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the workflow to be executed.</summary>
    public Guid WorkflowId { get; private set; }

    /// <summary>Gets the tenant that owns this execution.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the trigger source. One of: "manual", "webhook", "cron".
    /// </summary>
    public string TriggeredBy { get; private set; } = default!;

    /// <summary>Gets the JSON payload passed to the workflow execution.</summary>
    public string Payload { get; private set; } = default!;

    /// <summary>Gets the current lifecycle status of this item.</summary>
    public ExecutionQueueItemStatus Status { get; private set; }

    /// <summary>Gets the UTC timestamp when this item was enqueued.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets the UTC timestamp when a worker picked up this item, or <c>null</c> if not yet claimed.</summary>
    public DateTimeOffset? PickedUpAt { get; private set; }

    /// <summary>Gets the UTC timestamp when processing finished (success or failure), or <c>null</c> if still in progress.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    // Required by EF Core
    private ExecutionQueueItem() { }

    /// <summary>
    /// Creates a new <see cref="ExecutionQueueItem"/> with <see cref="ExecutionQueueItemStatus.Pending"/> status.
    /// </summary>
    /// <param name="workflowId">The workflow to execute.</param>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="triggeredBy">How the execution was triggered (manual/webhook/cron).</param>
    /// <param name="payload">JSON payload for the workflow execution.</param>
    /// <returns>A new pending queue item.</returns>
    public static ExecutionQueueItem Create(Guid workflowId, Guid tenantId, string triggeredBy, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(triggeredBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new ExecutionQueueItem
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            TenantId = tenantId,
            TriggeredBy = triggeredBy,
            Payload = payload,
            Status = ExecutionQueueItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Transitions the item to <see cref="ExecutionQueueItemStatus.Processing"/> and records the pick-up time.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the item is not in <see cref="ExecutionQueueItemStatus.Pending"/> status.</exception>
    public void MarkProcessing()
    {
        if (Status != ExecutionQueueItemStatus.Pending)
            throw new InvalidOperationException($"Cannot mark item as Processing when status is {Status}.");
        Status = ExecutionQueueItemStatus.Processing;
        PickedUpAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Transitions the item to <see cref="ExecutionQueueItemStatus.Done"/> and records the completion time.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the item is not in <see cref="ExecutionQueueItemStatus.Processing"/> status.</exception>
    public void MarkCompleted()
    {
        if (Status != ExecutionQueueItemStatus.Processing)
            throw new InvalidOperationException($"Cannot mark item as Done when status is {Status}.");
        Status = ExecutionQueueItemStatus.Done;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Transitions the item to <see cref="ExecutionQueueItemStatus.Failed"/> and records the completion time.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the item is not in <see cref="ExecutionQueueItemStatus.Processing"/> status.</exception>
    public void MarkFailed()
    {
        if (Status != ExecutionQueueItemStatus.Processing)
            throw new InvalidOperationException($"Cannot mark item as Failed when status is {Status}.");
        Status = ExecutionQueueItemStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
