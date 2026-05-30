using OrchestFlowAI.Domain.Entities;
namespace OrchestFlowAI.Application.Abstractions;
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, Guid tenantId, CancellationToken ct = default);
    /// <summary>Finds a user by email across all tenants — used for login where tenant is unknown.</summary>
    Task<User?> GetByEmailGlobalAsync(string email, CancellationToken ct = default);
    Task<User?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<User> CreateAsync(User user, CancellationToken ct = default);
}