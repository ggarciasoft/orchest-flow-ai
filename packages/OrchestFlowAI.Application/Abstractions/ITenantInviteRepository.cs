using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Application.Abstractions;

/// <summary>
/// Repository for tenant invite persistence and retrieval.
/// </summary>
public interface ITenantInviteRepository
{
    /// <summary>Gets an invite by its token value.</summary>
    Task<TenantInvite?> GetByTokenAsync(string token, CancellationToken ct = default);

    /// <summary>Gets a specific invite by id, scoped to a tenant.</summary>
    Task<TenantInvite?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    /// <summary>Creates and persists a new tenant invite.</summary>
    Task<TenantInvite> CreateAsync(TenantInvite invite, CancellationToken ct = default);

    /// <summary>Persists changes to an existing invite (e.g. after acceptance).</summary>
    Task UpdateAsync(TenantInvite invite, CancellationToken ct = default);

    /// <summary>Lists all invites for a tenant, ordered by creation date descending.</summary>
    Task<IReadOnlyList<TenantInvite>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Deletes (revokes) an invite by id, scoped to a tenant.</summary>
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
}
