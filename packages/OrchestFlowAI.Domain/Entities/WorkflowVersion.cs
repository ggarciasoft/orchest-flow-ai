namespace OrchestFlowAI.Domain.Entities;

/// <summary>
/// Represents a specific version of a workflow within the OrchestFlowAI system.
/// </summary>
public sealed class WorkflowVersion
{
    /// <summary>
    /// Gets the unique identifier for this workflow version.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the workflow associated with this version.
    /// </summary>
    public Guid WorkflowId { get; private set; }

    /// <summary>
    /// Gets the version number of the workflow version.
    /// </summary>
    public int VersionNumber { get; private set; }

    /// <summary>
    /// Gets the workflow definition in serialized JSON format.
    /// </summary>
    public string DefinitionJson { get; private set; } = default!;

    /// <summary>
    /// Indicates whether this workflow version is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who created this workflow version.
    /// </summary>
    public Guid CreatedBy { get; private set; }

    /// <summary>
    /// Gets the date and time when this workflow version was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowVersion"/> class.
    /// </summary>
    private WorkflowVersion() { }

    /// <summary>
    /// Factory method to create a new workflow version.
    /// </summary>
    /// <param name="workflowId">The identifier of the workflow associated with this version.</param>
    /// <param name="versionNumber">The version number of the new workflow version.</param>
    /// <param name="definitionJson">The JSON definition of the workflow for this version.</param>
    /// <param name="createdBy">The identifier of the user who created this workflow version.</param>
    /// <returns>A newly created instance of WorkflowVersion.</returns>
    public static WorkflowVersion Create(Guid workflowId, int versionNumber, string definitionJson, Guid createdBy)
        => new()
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            VersionNumber = versionNumber,
            DefinitionJson = definitionJson,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Marks this workflow version as active.
    /// </summary>
    public void Activate() => IsActive = true;

    /// <summary>
    /// Marks this workflow version as inactive.
    /// </summary>
    public void Deactivate() => IsActive = false;
}