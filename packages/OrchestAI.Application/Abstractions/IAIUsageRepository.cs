using OrchestAI.Domain.Entities;
namespace OrchestAI.Application.Abstractions;
public interface IAIUsageRepository
{
    Task<AIUsageLog> CreateAsync(AIUsageLog log, CancellationToken ct = default);
    Task<IReadOnlyList<AIUsageLog>> GetByExecutionAsync(Guid executionId, CancellationToken ct = default);
}