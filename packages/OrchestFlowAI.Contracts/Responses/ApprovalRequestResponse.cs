namespace OrchestFlowAI.Contracts.Responses;
public sealed record ApprovalRequestResponse(
    Guid Id,
    Guid WorkflowExecutionId,
    Guid NodeExecutionId,
    string Status,
    string PayloadJson,
    DateTime RequestedAt,
    DateTime? RespondedAt,
    string? Decision,
    string? Comment,
    // Enriched context — populated by the API layer
    string? WorkflowName = null,
    Guid? WorkflowId = null,
    int? WorkflowVersionNumber = null,
    int? FormVersionNumber = null);