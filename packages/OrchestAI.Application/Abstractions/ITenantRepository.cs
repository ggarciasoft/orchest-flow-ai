using OrchestAI.Domain.Entities;

namespace OrchestAI.Application.Abstractions;

/// <summary>
/// Repository for tenant management operations.
/// </summary>
public interface ITenantRepository
{
    /// <summary>Gets a tenant by id.</summary>
    Task<Tenant?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates a new tenant and persists it.</summary>
    Task<Tenant> CreateAsync(Tenant tenant, CancellationToken ct = default);
}
