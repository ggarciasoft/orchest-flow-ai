namespace OrchestAI.Domain.Entities;

/// <summary>
/// Represents a workflow definition owned by a tenant.
/// </summary>
public sealed class Workflow
{
    /// <summary>
    /// Gets the unique identifier of the workflow.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the tenant that owns the workflow.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the name of the workflow.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Gets the description of the workflow.
    /// </summary>
    public string Description { get; private set; } = default!;

    /// <summary>
    /// Gets the unique identifier of the user who created the workflow.
    /// </summary>
    public Guid CreatedBy { get; private set; }

    /// <summary>
    /// Gets the date and time when the workflow was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the date and time when the workflow was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Indicates whether the workflow is deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets the date and time when the workflow was deleted, if applicable.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Workflow"/> class.
    /// </summary>
    private Workflow() { }

    /// <summary>
    /// Creates a new workflow with the given details.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant that owns the workflow.</param>
    /// <param name="name">The human-readable name of the workflow.</param>
    /// <param name="description">The description of the workflow.</param>
    /// <param name="createdBy">The unique identifier of the user who created the workflow.</param>
    /// <returns>A new <see cref="Workflow"/> instance with a generated identifier.</returns>
    public static Workflow Create(Guid tenantId, string name, string description, Guid createdBy)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Updates the name and description of the workflow.
    /// </summary>
    /// <param name="name">The new name of the workflow.</param>
    /// <param name="description">The new description of the workflow.</param>
    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the workflow as deleted.
    /// </summary>
    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restores the workflow to an active state.
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}