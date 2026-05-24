using OrchestAI.Contracts;
namespace OrchestAI.Contracts.Requests;

/// <summary>Request DTO for updating workflow trigger settings.</summary>
public sealed record UpdateWorkflowTriggerRequest(
    TriggerType TriggerType,
    string? WebhookSecret,
    string? CronExpression);
