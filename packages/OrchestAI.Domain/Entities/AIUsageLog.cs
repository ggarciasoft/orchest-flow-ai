namespace OrchestAI.Domain.Entities;
public sealed class AIUsageLog
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid WorkflowExecutionId { get; private set; }
    public Guid NodeExecutionId { get; private set; }
    public string Provider { get; private set; } = default!;
    public string Model { get; private set; } = default!;
    public string PromptVersion { get; private set; } = default!;
    public int PromptTokens { get; private set; }
    public int CompletionTokens { get; private set; }
    public int TotalTokens { get; private set; }
    public decimal? EstimatedCostUsd { get; private set; }
    public DateTime CreatedAt { get; private set; }
    private AIUsageLog() { }
    public static AIUsageLog Create(Guid tenantId, Guid workflowExecutionId, Guid nodeExecutionId, string provider, string model, string promptVersion, int promptTokens, int completionTokens, decimal? estimatedCostUsd = null)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, WorkflowExecutionId = workflowExecutionId, NodeExecutionId = nodeExecutionId, Provider = provider, Model = model, PromptVersion = promptVersion, PromptTokens = promptTokens, CompletionTokens = completionTokens, TotalTokens = promptTokens + completionTokens, EstimatedCostUsd = estimatedCostUsd, CreatedAt = DateTime.UtcNow };
}