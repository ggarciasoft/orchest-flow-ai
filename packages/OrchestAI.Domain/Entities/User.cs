using OrchestAI.Domain.Enums;
namespace OrchestAI.Domain.Entities;

/// <summary>
/// Represents a user account within a tenant.
/// Users are scoped to a single tenant and carry a role that governs their access level.
/// </summary>
public sealed class User
{
    /// <summary>Unique identifier for this user.</summary>
    public Guid Id { get; private set; }

    /// <summary>The tenant this user belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The user's email address, used for authentication.</summary>
    public string Email { get; private set; } = default!;

    /// <summary>Human-readable display name shown in the UI.</summary>
    public string DisplayName { get; private set; } = default!;

    /// <summary>Role governing what the user is permitted to do within OrchestAI.</summary>
    public UserRole Role { get; private set; }

    /// <summary>Bcrypt or equivalent hash of the user's password. Never store plaintext.</summary>
    public string PasswordHash { get; private set; } = default!;

    /// <summary>UTC timestamp when the user account was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Private constructor — use <see cref="Create"/> factory method.</summary>
    private User() { }

    /// <summary>
    /// Creates a new user account for the specified tenant.
    /// </summary>
    /// <param name="tenantId">The tenant this user belongs to.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="displayName">Human-readable name shown in the UI.</param>
    /// <param name="role">Access role for this user.</param>
    /// <param name="passwordHash">Pre-hashed password (never raw plaintext).</param>
    /// <returns>A new <see cref="User"/> with a generated Id.</returns>
    public static User Create(Guid tenantId, string email, string displayName, UserRole role, string passwordHash)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, Email = email, DisplayName = displayName, Role = role, PasswordHash = passwordHash, CreatedAt = DateTime.UtcNow };
}
