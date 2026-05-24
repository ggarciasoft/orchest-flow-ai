using Microsoft.AspNetCore.SignalR;
using OrchestFlowAI.Api.Hubs;
using OrchestFlowAI.Contracts.Notifications;

namespace OrchestFlowAI.Api.Notifications;

/// <summary>
/// SignalR implementation of <see cref="IExecutionNotifier"/>.
/// Sends execution lifecycle events to all clients subscribed to the relevant execution group.
/// Registered in the API composition root (Program.cs) when SignalR is available.
/// </summary>
public sealed class SignalRExecutionNotifier : IExecutionNotifier
{
    private readonly IHubContext<ExecutionHub> _hub;

    /// <summary>
    /// Initializes a new instance of <see cref="SignalRExecutionNotifier"/>.
    /// </summary>
    /// <param name="hub">The SignalR hub context used to send messages to clients.</param>
    public SignalRExecutionNotifier(IHubContext<ExecutionHub> hub)
    {
        _hub = hub;
    }

    /// <inheritdoc/>
    public Task NotifyNodeStarted(Guid executionId, Guid nodeId, string nodeType, CancellationToken ct = default)
        => _hub.Clients.Group($"execution:{executionId}")
               .SendAsync("NodeStarted", new { executionId, nodeId, nodeType }, ct);

    /// <inheritdoc/>
    public Task NotifyNodeCompleted(Guid executionId, Guid nodeId, string nodeType, CancellationToken ct = default)
        => _hub.Clients.Group($"execution:{executionId}")
               .SendAsync("NodeCompleted", new { executionId, nodeId, nodeType }, ct);

    /// <inheritdoc/>
    public Task NotifyNodeFailed(Guid executionId, Guid nodeId, string nodeType, string error, CancellationToken ct = default)
        => _hub.Clients.Group($"execution:{executionId}")
               .SendAsync("NodeFailed", new { executionId, nodeId, nodeType, error }, ct);

    /// <inheritdoc/>
    public Task NotifyExecutionCompleted(Guid executionId, string status, CancellationToken ct = default)
        => _hub.Clients.Group($"execution:{executionId}")
               .SendAsync("ExecutionCompleted", new { executionId, status }, ct);
}
