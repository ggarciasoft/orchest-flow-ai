namespace OrchestAI.Contracts.Notifications;

/// <summary>
/// Abstraction for broadcasting real-time execution lifecycle events to connected clients.
/// Implementations may send events over SignalR, in-memory channels, or no-ops for testing.
/// </summary>
public interface IExecutionNotifier
{
    /// <summary>
    /// Notifies that a node has started executing.
    /// </summary>
    /// <param name="executionId">The workflow execution identifier.</param>
    /// <param name="nodeId">The node identifier within the workflow definition.</param>
    /// <param name="nodeType">The node type string (e.g. <c>ai.summarize</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyNodeStarted(Guid executionId, Guid nodeId, string nodeType, CancellationToken ct = default);

    /// <summary>
    /// Notifies that a node has completed successfully.
    /// </summary>
    /// <param name="executionId">The workflow execution identifier.</param>
    /// <param name="nodeId">The node identifier within the workflow definition.</param>
    /// <param name="nodeType">The node type string.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyNodeCompleted(Guid executionId, Guid nodeId, string nodeType, CancellationToken ct = default);

    /// <summary>
    /// Notifies that a node has failed after all retries are exhausted.
    /// </summary>
    /// <param name="executionId">The workflow execution identifier.</param>
    /// <param name="nodeId">The node identifier within the workflow definition.</param>
    /// <param name="nodeType">The node type string.</param>
    /// <param name="error">The error message describing the failure.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyNodeFailed(Guid executionId, Guid nodeId, string nodeType, string error, CancellationToken ct = default);

    /// <summary>
    /// Notifies that the entire workflow execution has finished (succeeded, failed, or paused).
    /// </summary>
    /// <param name="executionId">The workflow execution identifier.</param>
    /// <param name="status">Final status string (e.g. <c>Completed</c>, <c>Failed</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifyExecutionCompleted(Guid executionId, string status, CancellationToken ct = default);
}
