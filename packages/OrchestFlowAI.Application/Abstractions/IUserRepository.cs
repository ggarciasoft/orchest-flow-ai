using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;

namespace OrchestFlowAI.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, Guid tenantId, CancellationToken ct = default);
    /// <summary>Finds a user by email across all tenants — used for login where tenant is unknown.</summary>
    Task<User?> GetByEmailGlobalAsync(string email, CancellationToken ct = default);
    Task<User?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<User> CreateAsync(User user, CancellationToken ct = default);
    Task<IReadOnlyList<User>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task UpdateRoleAsync(Guid userId, Guid tenantId, UserRole role, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
}