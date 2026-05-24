namespace OrchestAI.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a persistent execution queue item.
/// </summary>
public enum ExecutionQueueItemStatus
{
    /// <summary>Item has been enqueued and is waiting to be picked up by a worker.</summary>
    Pending,

    /// <summary>Item has been claimed by a worker and is currently being processed.</summary>
    Processing,

    /// <summary>Item was processed successfully.</summary>
    Done,

    /// <summary>Item processing failed and will not be retried automatically.</summary>
    Failed
}
