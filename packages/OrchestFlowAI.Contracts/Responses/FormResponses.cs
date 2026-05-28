namespace OrchestFlowAI.Contracts.Responses;

public sealed record FormResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string Slug,
    string? Description,
    string FieldsJson,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record FormSubmissionResponse(
    Guid Id,
    Guid FormId,
    Guid WorkflowExecutionId,
    string NodeExecutionId,
    string ValuesJson,
    DateTime SubmittedAt);
