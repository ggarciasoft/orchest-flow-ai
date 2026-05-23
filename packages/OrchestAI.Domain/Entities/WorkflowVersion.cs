namespace OrchestAI.Domain.Entities;
public sealed class WorkflowVersion
{
    public Guid Id { get; private set; }
    public Guid WorkflowId { get; private set; }
    public int VersionNumber { get; private set; }
    public string DefinitionJson { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    private WorkflowVersion() { }
    public static WorkflowVersion Create(Guid workflowId, int versionNumber, string definitionJson, Guid createdBy)
        => new() { Id = Guid.NewGuid(), WorkflowId = workflowId, VersionNumber = versionNumber, DefinitionJson = definitionJson, CreatedBy = createdBy, CreatedAt = DateTime.UtcNow };
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}