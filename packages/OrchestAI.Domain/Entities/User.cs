using OrchestAI.Domain.Enums;
namespace OrchestAI.Domain.Entities;
public sealed class User
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public string PasswordHash { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    private User() { }
    public static User Create(Guid tenantId, string email, string displayName, UserRole role, string passwordHash)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, Email = email, DisplayName = displayName, Role = role, PasswordHash = passwordHash, CreatedAt = DateTime.UtcNow };
}