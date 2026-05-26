namespace OrchestFlowAI.Domain.Entities;

/// <summary>
/// A named encrypted secret scoped to a tenant.
/// The value is always stored encrypted; never returned to the client.
/// </summary>
public sealed class Secret
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    /// <summary>Friendly name used to reference this secret, e.g. "openai-key".</summary>
    public string Name { get; private set; } = default!;
    /// <summary>AES-256 encrypted value — never returned to clients.</summary>
    public string EncryptedValue { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Secret() { }

    public static Secret Create(Guid tenantId, string name, string encryptedValue)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, Name = name, EncryptedValue = encryptedValue, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

    public void UpdateValue(string encryptedValue)
    {
        EncryptedValue = encryptedValue;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }
}
