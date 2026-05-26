namespace OrchestFlowAI.Domain.Entities;

public sealed class GmailCredential
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    /// <summary>Friendly name used to reference this credential in workflow nodes (e.g. "my-gmail").</summary>
    public string Name { get; private set; } = default!;
    public string ClientId { get; private set; } = default!;
    public string ClientSecret { get; private set; } = default!;
    public string RefreshToken { get; private set; } = default!;
    /// <summary>Gmail address populated after the OAuth callback completes.</summary>
    public string? Email { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private GmailCredential() { }

    public static GmailCredential Create(Guid tenantId, string name, string clientId, string clientSecret, string refreshToken, string? email = null)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            ClientId = clientId,
            ClientSecret = clientSecret,
            RefreshToken = refreshToken,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    public void UpdateTokens(string refreshToken, string? email)
    {
        RefreshToken = refreshToken;
        if (email != null) Email = email;
        UpdatedAt = DateTime.UtcNow;
    }
}
