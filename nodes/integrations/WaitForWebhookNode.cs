using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.Integrations;

/// <summary>
/// Suspends a workflow execution and issues a one-time URL-safe token.
/// An external system can resume the workflow by POSTing to /api/webhooks/resume/{token}.
/// </summary>
public sealed class WaitForWebhookNode : IWorkflowNode
{
    public string Type => "integrations.wait-for-webhook";

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var tokenRepo = ctx.Services.GetRequiredService<ICorrelationTokenRepository>();

        var timeoutSeconds = ctx.GetConfig<int?>("timeoutSeconds");
        TimeSpan? ttl = timeoutSeconds.HasValue ? TimeSpan.FromSeconds(timeoutSeconds.Value) : null;

        var correlationToken = CorrelationToken.Create(
            ctx.ExecutionId,
            ctx.NodeExecutionId,
            ctx.TenantId,
            "wait",
            ttl);

        await tokenRepo.CreateAsync(correlationToken, ct);

        var outputs = new Dictionary<string, object?>
        {
            ["_correlationToken"] = correlationToken.Token,
            ["_resumeUrl"] = $"/api/webhooks/resume/{correlationToken.Token}"
        };

        return NodeExecutionResult.WaitingForApproval(outputs);
    }
}

/// <summary>Descriptor for <see cref="WaitForWebhookNode"/>.</summary>
public sealed class WaitForWebhookNodeDescriptor : IWorkflowNodeDescriptor
{
    public string Type => "integrations.wait-for-webhook";
    public string DisplayName => "Wait for Webhook";
    public string Description => "Pauses execution and waits for an external system to POST a resume payload.";
    public string Category => "integrations";
    public string Version => "1.0.0";
    public string? IconKey => "webhook";

    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();

    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("_correlationToken", "Correlation Token", "Token for the resume URL.", DataType.String),
        new NodeOutputDefinition("_resumeUrl", "Resume URL", "URL external system POSTs to.", DataType.String),
        new NodeOutputDefinition("_resumedAt", "Resumed At", "ISO timestamp when resumed.", DataType.String),
    };

    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("timeoutSeconds", "Timeout (seconds)", "Optional TTL for the token.", DataType.Number, Required: false),
    };
}
