namespace OrchestAI.Domain.Entities;
public sealed class Workflow
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    private Workflow() { }
    public static Workflow Create(Guid tenantId, string name, string description, Guid createdBy)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, Name = name, Description = description, CreatedBy = createdBy, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
    public void Update(string name, string description) { Name = name; Description = description; UpdatedAt = DateTime.UtcNow; }
    public void Delete() { IsDeleted = true; DeletedAt = DateTime.UtcNow; UpdatedAt = DateTime.UtcNow; }
    public void Restore() { IsDeleted = false; DeletedAt = null; UpdatedAt = DateTime.UtcNow; }
}