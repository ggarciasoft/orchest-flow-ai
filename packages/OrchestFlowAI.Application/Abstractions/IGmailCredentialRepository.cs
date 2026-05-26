using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Application.Abstractions;

public interface IGmailCredentialRepository
{
    Task<GmailCredential?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<GmailCredential?> GetByNameAsync(string name, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<GmailCredential>> ListAsync(Guid tenantId, CancellationToken ct = default);
    Task<GmailCredential> CreateAsync(GmailCredential credential, CancellationToken ct = default);
    Task UpdateAsync(GmailCredential credential, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
}
