using OrchestFlowAI.Domain.ValueObjects;

namespace OrchestFlowAI.Domain.Entities;

/// <summary>
/// Represents an isolated tenant (organization) within OrchestFlowAI.
/// All workflows, executions, and users are scoped to a tenant to enforce data isolation.
/// </summary>
public sealed class Tenant
{
    /// <summary>Unique identifier for this tenant.</summary>
    public Guid Id { get; private set; }

    /// <summary>Human-readable name of the organization.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>UTC timestamp when the tenant was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Per-tenant configuration (branding, limits, feature flags). Serialized as JSON.</summary>
    public string ConfigJson { get; private set; } = "{}";

    /// <summary>Private constructor — use <see cref="Create"/> factory method.</summary>
    private Tenant() { }

    /// <summary>
    /// Creates a new tenant with the given organization name.
    /// </summary>
    public static Tenant Create(string name) => new() { Id = Guid.NewGuid(), Name = name, CreatedAt = DateTime.UtcNow };

    /// <summary>Persists updated configuration by serializing to JSON.</summary>
    public void UpdateConfig(TenantConfig config)
        => ConfigJson = System.Text.Json.JsonSerializer.Serialize(config);

    /// <summary>Deserializes the stored JSON into a <see cref="TenantConfig"/> instance.</summary>
    public TenantConfig GetConfig()
    {
        try { return System.Text.Json.JsonSerializer.Deserialize<TenantConfig>(ConfigJson) ?? TenantConfig.Default(); }
        catch { return TenantConfig.Default(); }
    }
}
