using OrchestFlowAI.Contracts.Events;
namespace OrchestFlowAI.Application.Abstractions;
public interface IExecutionQueue
{
    Task EnqueueAsync(ExecutionQueueMessage message, CancellationToken ct = default);
    Task EnqueueResumeAsync(ExecutionResumeMessage message, CancellationToken ct = default);
}