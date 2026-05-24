using OrchestAI.Contracts.Notifications;

namespace OrchestAI.Infrastructure.Notifications;

/// <summary>
/// No-op implementation of <see cref="IExecutionNotifier"/> used when SignalR is not configured
/// (e.g. local development without a database connection string, or unit tests).
/// </summary>
public sealed class StubExecutionNotifier : IExecutionNotifier
{
    /// <inheritdoc/>
    public Task NotifyNodeStarted(Guid executionId, Guid nodeId, string nodeType, CancellationToken ct = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task NotifyNodeCompleted(Guid executionId, Guid nodeId, string nodeType, CancellationToken ct = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task NotifyNodeFailed(Guid executionId, Guid nodeId, string nodeType, string error, CancellationToken ct = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task NotifyExecutionCompleted(Guid executionId, string status, CancellationToken ct = default)
        => Task.CompletedTask;
}

