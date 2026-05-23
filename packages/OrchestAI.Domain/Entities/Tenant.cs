namespace OrchestAI.Domain.Entities;

/// <summary>
/// Represents an isolated tenant (organization) within OrchestAI.
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

    /// <summary>Private constructor — use <see cref="Create"/> factory method.</summary>
    private Tenant() { }

    /// <summary>
    /// Creates a new tenant with the given organization name.
    /// </summary>
    /// <param name="name">The organization or tenant name.</param>
    /// <returns>A new <see cref="Tenant"/> with a generated Id.</returns>
    public static Tenant Create(string name) => new() { Id = Guid.NewGuid(), Name = name, CreatedAt = DateTime.UtcNow };
}
