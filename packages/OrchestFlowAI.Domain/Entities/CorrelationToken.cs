namespace OrchestFlowAI.Domain.Entities;

/// <summary>
/// Represents a one-time correlation token used to resume a workflow execution
/// from an external system (webhook wait or gate).
/// </summary>
public sealed class CorrelationToken
{
    public Guid Id { get; private set; }
    public string Token { get; private set; } = default!;
    public Guid ExecutionId { get; private set; }
    public Guid NodeExecutionId { get; private set; }
    public Guid TenantId { get; private set; }
    /// <summary>"wait" for WaitForWebhookNode, "gate" for ExternalGateNode.</summary>
    public string Kind { get; private set; } = default!;
    public bool Used { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CorrelationToken() { }

    public static CorrelationToken Create(Guid executionId, Guid nodeExecutionId, Guid tenantId, string kind, TimeSpan? ttl = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Token = Guid.NewGuid().ToString("N"),
            ExecutionId = executionId,
            NodeExecutionId = nodeExecutionId,
            TenantId = tenantId,
            Kind = kind,
            Used = false,
            ExpiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null,
            CreatedAt = DateTime.UtcNow
        };

    public void MarkUsed() { Used = true; }
}
