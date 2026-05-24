namespace OrchestFlowAI.Contracts.Responses;
public sealed record WorkflowResponse(
    Guid Id,
    string Name,
    string Description,
    int? ActiveVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    OrchestFlowAI.Contracts.TriggerType TriggerType,
    string? WebhookSecret,
    string? CronExpression,
    int RetryMaxAttempts = 0,
    int RetryBackoffMs = 0,
    double RetryBackoffMultiplier = 2.0);