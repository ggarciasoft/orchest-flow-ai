using OrchestAI.Domain.Entities;

namespace OrchestAI.Application.Abstractions;

/// <summary>
/// Repository for tenant invite persistence and retrieval.
/// </summary>
public interface ITenantInviteRepository
{
    /// <summary>Gets an invite by its token value.</summary>
    Task<TenantInvite?> GetByTokenAsync(string token, CancellationToken ct = default);

    /// <summary>Creates and persists a new tenant invite.</summary>
    Task<TenantInvite> CreateAsync(TenantInvite invite, CancellationToken ct = default);

    /// <summary>Persists changes to an existing invite (e.g. after acceptance).</summary>
    Task UpdateAsync(TenantInvite invite, CancellationToken ct = default);
}
