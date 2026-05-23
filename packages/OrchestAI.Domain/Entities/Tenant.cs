namespace OrchestAI.Domain.Entities;
public sealed class Tenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    private Tenant() { }
    public static Tenant Create(string name) => new() { Id = Guid.NewGuid(), Name = name, CreatedAt = DateTime.UtcNow };
}