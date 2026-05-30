namespace OrchestFlowAI.Domain.Entities;

/// <summary>
/// A persistent key-value configuration entry scoped to a tenant.
/// Workflows can read and write these values using system.read-config and system.write-config nodes.
/// </summary>
public sealed class WorkflowConfig
{
    public Guid    Id          { get; private set; }
    public Guid    TenantId    { get; private set; }
    /// <summary>Unique key within the tenant, e.g. "gmail.last_sync_date".</summary>
    public string  Key         { get; private set; } = default!;
    /// <summary>Value stored as a string regardless of type.</summary>
    public string  Value       { get; private set; } = default!;
    /// <summary>Declared type: string | number | boolean | json | datetime</summary>
    public string  ValueType   { get; private set; } = "string";
    public string? Description { get; private set; }
    public DateTime CreatedAt  { get; private set; }
    public DateTime UpdatedAt  { get; private set; }

    private WorkflowConfig() { }

    public static WorkflowConfig Create(Guid tenantId, string key, string value, string valueType = "string", string? description = null)
        => new()
        {
            Id          = Guid.NewGuid(),
            TenantId    = tenantId,
            Key         = key.Trim(),
            Value       = value,
            ValueType   = valueType,
            Description = description,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow,
        };

    public void Update(string value, string? description)
    {
        Value       = value;
        Description = description;
        UpdatedAt   = DateTime.UtcNow;
    }
}
