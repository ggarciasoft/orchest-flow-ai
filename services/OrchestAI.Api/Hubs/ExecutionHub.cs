using Microsoft.AspNetCore.SignalR;

namespace OrchestAI.Api.Hubs;

/// <summary>
/// SignalR hub for real-time workflow execution log streaming.
/// Clients join per-execution groups to receive live node lifecycle events.
/// </summary>
public sealed class ExecutionHub : Hub
{
    /// <summary>
    /// Adds the calling client to the group for the given execution,
    /// enabling it to receive all subsequent execution events.
    /// </summary>
    /// <param name="executionId">The execution ID to subscribe to.</param>
    public async Task JoinExecution(string executionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"execution:{executionId}");
    }

    /// <summary>
    /// Removes the calling client from the group for the given execution.
    /// </summary>
    /// <param name="executionId">The execution ID to unsubscribe from.</param>
    public async Task LeaveExecution(string executionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"execution:{executionId}");
    }
}
