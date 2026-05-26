namespace OrchestFlowAI.Domain.Entities;

/// <summary>
/// A key-value configuration entry scoped to a tenant.
/// Used to store runtime-configurable settings such as LLM provider API keys.
/// </summary>
public sealed class PlatformSetting
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Key { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public DateTime UpdatedAt { get; private set; }

    private PlatformSetting() { }

    public static PlatformSetting Create(Guid tenantId, string key, string value)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, Key = key, Value = value, UpdatedAt = DateTime.UtcNow };

    public void SetValue(string value)
    {
        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }
}
