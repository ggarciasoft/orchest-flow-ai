using OrchestFlowAI.Contracts.Events;

namespace OrchestFlowAI.Application.Abstractions;

/// <summary>
/// Read side of the execution queue — consumed by background workers.
/// Implementations may be in-memory (single-process) or Redis-backed (distributed).
/// </summary>
public interface IExecutionQueueConsumer
{
    /// <summary>Reads all execution messages as an async stream until cancellation.</summary>
    IAsyncEnumerable<ExecutionQueueMessage> ReadAllAsync(CancellationToken ct = default);

    /// <summary>Reads all resume messages as an async stream until cancellation.</summary>
    IAsyncEnumerable<ExecutionResumeMessage> ReadAllResumeAsync(CancellationToken ct = default);
}
