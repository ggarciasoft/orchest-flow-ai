using OrchestFlowAI.Domain.Entities;
namespace OrchestFlowAI.Application.Abstractions;
public interface IAIUsageRepository
{
    Task<AIUsageLog> CreateAsync(AIUsageLog log, CancellationToken ct = default);
    Task<IReadOnlyList<AIUsageLog>> GetByExecutionAsync(Guid executionId, CancellationToken ct = default);
}