using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;

namespace OrchestAI.Nodes.Integrations;

/// <summary>
/// POSTs a JSON payload containing execution context and optional node inputs to a configured URL.
/// Useful for triggering external systems at specific points in a workflow.
/// </summary>
public sealed class WebhookOutNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "integrations.webhook-out";

    /// <summary>
    /// Builds and sends a webhook payload to the configured URL.
    /// </summary>
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var client = ctx.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
        var url = ctx.GetConfig<string>("url") ?? throw new InvalidOperationException("url config is required");
        var includeInputs = ctx.GetConfig<string>("includeInputs") ?? "all";

        var payload = new Dictionary<string, object?>
        {
            ["executionId"] = ctx.ExecutionId.ToString(),
            ["correlationId"] = ctx.CorrelationId,
            ["step"] = ctx.Step
        };

        // Optionally include all node inputs in the outbound payload
        if (includeInputs == "all")
            payload["inputs"] = ctx.NodeInputs.ToDictionary(kv => kv.Key, kv => kv.Value);

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            global::System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync(url, content, ct);
        return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
        {
            ["statusCode"] = (int)response.StatusCode,
            ["sent"] = true
        });
    }
}

/// <summary>Descriptor for <see cref="WebhookOutNode"/>.</summary>
public sealed class WebhookOutNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "integrations.webhook-out";
    /// <inheritdoc />
    public string DisplayName => "Webhook Out";
    /// <inheritdoc />
    public string Description => "POSTs execution details to a configured webhook URL.";
    /// <inheritdoc />
    public string Category => "integrations";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "send";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("statusCode", "Status Code", "HTTP response status code.", DataType.Number),
        new NodeOutputDefinition("sent", "Sent", "Whether the webhook was sent.", DataType.Boolean)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("url", "URL", "Webhook target URL.", DataType.String, Required: true),
        new NodeConfigDefinition("includeInputs", "Include Inputs", "Include node inputs in payload.", DataType.Enum, Required: false, DefaultValue: "all", AllowedValues: new[] { "all", "none" })
    };
}
