namespace OrchestFlowAI.Domain.Entities;

/// <summary>Represents a custom form definition for a tenant.</summary>
public sealed class Form
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public string FieldsJson { get; private set; } = "[]";
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Form() { }

    public static Form Create(Guid tenantId, string name, string slug, string? description, string fieldsJson) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = name,
        Slug = slug,
        Description = description,
        FieldsJson = fieldsJson,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public void Update(string name, string slug, string? description, string fieldsJson)
    {
        Name = name;
        Slug = slug;
        Description = description;
        FieldsJson = fieldsJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
