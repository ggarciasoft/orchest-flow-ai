using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.Integrations;

/// <summary>
/// Suspends execution pending an external approve/reject decision.
/// An external system POSTs to /api/webhooks/gate/{token} with { "approved": bool, "reason"?: string, "data"?: object }.
/// </summary>
public sealed class ExternalGateNode : IWorkflowNode
{
    public string Type => "integrations.external-gate";

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var tokenRepo = ctx.Services.GetRequiredService<ICorrelationTokenRepository>();

        var timeoutSeconds = ctx.GetConfig<int?>("timeoutSeconds");
        TimeSpan? ttl = timeoutSeconds.HasValue ? TimeSpan.FromSeconds(timeoutSeconds.Value) : null;

        var correlationToken = CorrelationToken.Create(
            ctx.ExecutionId,
            ctx.NodeExecutionId,
            ctx.TenantId,
            "gate",
            ttl);

        await tokenRepo.CreateAsync(correlationToken, ct);

        var outputs = new Dictionary<string, object?>
        {
            ["_correlationToken"] = correlationToken.Token,
            ["_resumeUrl"] = $"/api/webhooks/gate/{correlationToken.Token}"
        };

        return NodeExecutionResult.WaitingForApproval(outputs);
    }
}

/// <summary>Descriptor for <see cref="ExternalGateNode"/>.</summary>
public sealed class ExternalGateNodeDescriptor : IWorkflowNodeDescriptor
{
    public string Type => "integrations.external-gate";
    public string DisplayName => "External Gate";
    public string Description => "Pauses execution and waits for an external system to approve or reject via a gate URL.";
    public string Category => "integrations";
    public string Version => "1.0.0";
    public string? IconKey => "shield-check";

    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();

    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("approved", "Approved", "Whether the gate was approved.", DataType.Boolean),
        new NodeOutputDefinition("reason", "Reason", "Optional reason from the external system.", DataType.String),
        new NodeOutputDefinition("_correlationToken", "Correlation Token", "Token used for the gate URL.", DataType.String),
        new NodeOutputDefinition("_resumedAt", "Resumed At", "ISO timestamp when gate was resolved.", DataType.String),
    };

    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("timeoutSeconds", "Timeout (seconds)", "Optional TTL for the gate token.", DataType.Number, Required: false),
    };
}
