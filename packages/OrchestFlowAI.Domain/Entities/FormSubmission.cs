namespace OrchestFlowAI.Domain.Entities;

/// <summary>Represents a submitted response to a custom form, associated with a workflow execution.</summary>
public sealed class FormSubmission
{
    public Guid Id { get; private set; }
    public Guid FormId { get; private set; }
    public Guid WorkflowExecutionId { get; private set; }
    public string NodeExecutionId { get; private set; } = default!;
    public Guid TenantId { get; private set; }
    public Guid? SubmittedBy { get; private set; }
    public string ValuesJson { get; private set; } = "{}";
    public DateTime SubmittedAt { get; private set; }

    private FormSubmission() { }

    public static FormSubmission Create(Guid formId, Guid executionId, string nodeExecutionId, Guid tenantId, Guid? submittedBy, string valuesJson) => new()
    {
        Id = Guid.NewGuid(),
        FormId = formId,
        WorkflowExecutionId = executionId,
        NodeExecutionId = nodeExecutionId,
        TenantId = tenantId,
        SubmittedBy = submittedBy,
        ValuesJson = valuesJson,
        SubmittedAt = DateTime.UtcNow
    };
}
