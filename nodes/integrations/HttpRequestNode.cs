using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;

namespace OrchestAI.Nodes.Integrations;

/// <summary>
/// Performs an HTTP request to any external REST endpoint.
/// Supports GET, POST, PUT, DELETE, and PATCH with configurable headers, body, and timeout.
/// </summary>
public sealed class HttpRequestNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "integrations.http";

    /// <summary>
    /// Sends an HTTP request and returns the status code, response body, and success flag.
    /// Throws on non-2xx responses; caller can configure retry via the engine's retry policy.
    /// </summary>
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var client = ctx.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

        var url = ctx.GetConfig<string>("url") ?? throw new InvalidOperationException("url config is required");
        var method = ctx.GetConfig<string>("method")?.ToUpperInvariant() ?? "GET";
        var headersJson = ctx.GetConfig<string>("headers");
        var body = ctx.GetConfig<string>("body");
        var timeoutSeconds = (int)(ctx.GetConfig<double?>("timeoutSeconds") ?? 30.0);

        // Parse optional headers JSON object
        var headers = headersJson != null
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson) ?? new()
            : new Dictionary<string, string>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = !string.IsNullOrWhiteSpace(body) ? new StringContent(body) : null
        };
        foreach (var kv in headers)
            request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);

        var response = await client.SendAsync(request, cts.Token);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
        {
            ["statusCode"] = (int)response.StatusCode,
            ["responseBody"] = responseBody,
            ["success"] = response.IsSuccessStatusCode
        });
    }
}

/// <summary>Descriptor for <see cref="HttpRequestNode"/>.</summary>
public sealed class HttpRequestNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "integrations.http";
    /// <inheritdoc />
    public string DisplayName => "HTTP Request";
    /// <inheritdoc />
    public string Description => "Calls any external REST API endpoint.";
    /// <inheritdoc />
    public string Category => "integrations";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "globe";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("statusCode", "Status Code", "HTTP status code.", DataType.Number),
        new NodeOutputDefinition("responseBody", "Response Body", "Response body as string.", DataType.String),
        new NodeOutputDefinition("success", "Success", "True if 2xx status code.", DataType.Boolean)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("url", "URL", "The URL to call.", DataType.String, Required: true),
        new NodeConfigDefinition("method", "Method", "HTTP method.", DataType.Enum, Required: false, DefaultValue: "GET", AllowedValues: new[] { "GET", "POST", "PUT", "DELETE", "PATCH" }),
        new NodeConfigDefinition("headers", "Headers", "JSON object of request headers.", DataType.String, Required: false),
        new NodeConfigDefinition("body", "Body", "Request body for POST/PUT.", DataType.String, Required: false),
        new NodeConfigDefinition("timeoutSeconds", "Timeout (s)", "Request timeout in seconds.", DataType.Number, Required: false, DefaultValue: 30)
    };
}
