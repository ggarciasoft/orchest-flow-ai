using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Application.Abstractions;

public interface ISecretRepository
{
    Task<IReadOnlyList<Secret>> ListAsync(Guid tenantId, CancellationToken ct = default);
    Task<Secret?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Secret?> GetByNameAsync(string name, Guid tenantId, CancellationToken ct = default);
    Task<Secret> CreateAsync(Secret secret, CancellationToken ct = default);
    Task UpdateAsync(Secret secret, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
}
