namespace OrchestAI.Contracts.Responses;
public sealed record WorkflowResponse(
    Guid Id,
    string Name,
    string Description,
    int? ActiveVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    OrchestAI.Contracts.TriggerType TriggerType,
    string? WebhookSecret,
    string? CronExpression);