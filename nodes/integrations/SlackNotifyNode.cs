using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;

namespace OrchestAI.Nodes.Integrations;

/// <summary>
/// Sends a notification to a Slack channel using a webhook.
/// Supports placeholder resolution for message text and optional channel override.
/// </summary>
public sealed class SlackNotifyNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "integrations.slack";

    /// <inheritdoc />
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var client = ctx.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

        var webhookUrl = ctx.GetConfig<string>("webhookUrl") ?? throw new ArgumentException("webhookUrl config is required");
        var rawMessage = ctx.GetConfig<string>("message") ?? throw new ArgumentException("message config is required");
        var channel = ctx.GetConfig<string>("channel");

        var resolvedMessage = ResolvePlaceholders(rawMessage, ctx.NodeInputs);

        var payload = new Dictionary<string, object?>
        {
            ["text"] = resolvedMessage
        };

        if (!string.IsNullOrWhiteSpace(channel))
        {
            payload["channel"] = channel;
        }

        var requestBody = JsonSerializer.Serialize(payload);

        try
        {
            using var requestContent = new StringContent(requestBody);
            var response = await client.PostAsync(webhookUrl, requestContent, ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Slack webhook failed with status code {(int)response.StatusCode}");
            }

            return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
            {
                ["sent"] = true
            });
        }
        catch (Exception ex)
        {
            throw new Exception($"Slack webhook error: {ex.Message}");
        }
    }

    private static string ResolvePlaceholders(string template, IReadOnlyDictionary<string, object?> variables)
    {
        foreach (var key in variables.Keys)
        {
            template = template.Replace($"{{{{{key}}}}}", variables[key]?.ToString() ?? string.Empty);
        }
        return template;
    }
}

/// <summary>Descriptor for the <see cref="SlackNotifyNode"/>.</summary>
public sealed class SlackNotifyNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "integrations.slack";
    /// <inheritdoc />
    public string DisplayName => "Slack Notify";
    /// <inheritdoc />
    public string Description => "Sends a notification to Slack using a webhook.";
    /// <inheritdoc />
    public string Category => "integrations";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "message-square";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("sent", "Sent", "Whether the Slack message was sent successfully.", DataType.Boolean)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("webhookUrl", "Webhook URL", "Slack incoming webhook URL.", DataType.String, Required: true),
        new NodeConfigDefinition("message", "Message", "The message text (supports placeholders).", DataType.String, Required: true),
        new NodeConfigDefinition("channel", "Channel", "Optional channel override.", DataType.String, Required: false)
    };
}