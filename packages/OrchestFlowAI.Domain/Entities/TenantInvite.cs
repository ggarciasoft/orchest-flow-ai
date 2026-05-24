namespace OrchestFlowAI.Domain.Entities;

/// <summary>
/// Represents an invitation to join a tenant workspace.
/// A token is generated and shared with the invitee; they use it to create their account.
/// </summary>
public sealed class TenantInvite
{
    /// <summary>Unique identifier for this invite.</summary>
    public Guid Id { get; private set; }

    /// <summary>The tenant this invite belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Email address of the person being invited.</summary>
    public string Email { get; private set; } = default!;

    /// <summary>Role that will be assigned to the user on acceptance.</summary>
    public string Role { get; private set; } = default!;

    /// <summary>Secure random token sent to the invitee to redeem the invite.</summary>
    public string Token { get; private set; } = default!;

    /// <summary>UTC timestamp when this invite was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>UTC timestamp when this invite expires (24 hours after creation).</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>UTC timestamp when this invite was accepted, or null if not yet accepted.</summary>
    public DateTime? AcceptedAt { get; private set; }

    /// <summary>Private constructor — use <see cref="Create"/> factory method.</summary>
    private TenantInvite() { }

    /// <summary>
    /// Creates a new tenant invite for the given email and role.
    /// The token is a newly generated Guid. Invite expires in 24 hours.
    /// </summary>
    /// <param name="tenantId">The tenant the invitee is being invited to.</param>
    /// <param name="email">Email address of the invitee.</param>
    /// <param name="role">Role to assign on acceptance.</param>
    /// <returns>A new <see cref="TenantInvite"/> with a generated token.</returns>
    public static TenantInvite Create(Guid tenantId, string email, string role)
    {
        var now = DateTime.UtcNow;
        return new TenantInvite
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            Role = role,
            Token = Guid.NewGuid().ToString("N"),
            CreatedAt = now,
            ExpiresAt = now.AddHours(24),
        };
    }

    /// <summary>
    /// Returns true if the invite has expired and can no longer be accepted.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Returns true if the invite has already been accepted.
    /// </summary>
    public bool IsAccepted => AcceptedAt.HasValue;

    /// <summary>
    /// Marks the invite as accepted at the current UTC time.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the invite is already accepted or expired.</exception>
    public void Accept()
    {
        if (IsAccepted) throw new InvalidOperationException("Invite has already been accepted.");
        if (IsExpired) throw new InvalidOperationException("Invite has expired.");
        AcceptedAt = DateTime.UtcNow;
    }
}
