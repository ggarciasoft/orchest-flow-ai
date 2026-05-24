using System.Text.Json;
namespace OrchestFlowAI.Contracts.Requests;
public sealed record CreateWorkflowRequest(
    string Name,
    string Description,
    JsonElement Definition,
    TriggerType TriggerType = TriggerType.Manual,
    string? WebhookSecret = null,
    string? CronExpression = null,
    int RetryMaxAttempts = 0,
    int RetryBackoffMs = 1000,
    double RetryBackoffMultiplier = 2.0);