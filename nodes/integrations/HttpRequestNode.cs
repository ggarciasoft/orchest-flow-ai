using global::System.Net.Http;
using global::System.Text;
using global::System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.Integrations;

/// <summary>
/// Performs an HTTP request to any external REST endpoint with optional authentication support.
/// </summary>
public sealed class HttpRequestNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "integrations.http";

    /// <summary>
    /// Sends an HTTP request and returns the status code, response body, and success flag.
    /// </summary>
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var client = ctx.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

        var url = ctx.GetConfig<string>("url") ?? throw new InvalidOperationException("url config is required");
        var method = ctx.GetConfig<string>("method")?.ToUpperInvariant() ?? "GET";
        var headersJson = ctx.GetConfig<string>("headers");
        var body = ctx.GetConfig<string>("body");
        var timeoutSeconds = (int)(ctx.GetConfig<double?>("timeoutSeconds") ?? 30.0);
        var authType = ctx.GetConfig<string>("authType") ?? "none";

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

        // Apply authentication
        await ApplyAuth(request, authType, ctx, client, ct);

        var response = await client.SendAsync(request, cts.Token);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
        {
            ["statusCode"] = (int)response.StatusCode,
            ["responseBody"] = responseBody,
            ["success"] = response.IsSuccessStatusCode
        });
    }

    /// <summary>
    /// Applies authentication to the HTTP request based on the auth type.
    /// </summary>
    private async Task ApplyAuth(HttpRequestMessage request, string authType, WorkflowExecutionContext ctx, HttpClient client, CancellationToken ct)
    {
        string Resolve(string? template, IReadOnlyDictionary<string, object?> inputs)
        {
            if (string.IsNullOrWhiteSpace(template)) return string.Empty;
            return global::System.Text.RegularExpressions.Regex.Replace(template, @"{{(\w+)}}", match =>
            {
                var key = match.Groups[1].Value;
                return inputs.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
            });
        }

        switch (authType)
        {
            case "none":
                break;
            case "bearer":
                var token = Resolve(ctx.GetConfig<string>("authToken"), ctx.NodeInputs);
                request.Headers.Authorization = new global::System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                break;
            case "basic":
                var username = Resolve(ctx.GetConfig<string>("authUsername"), ctx.NodeInputs);
                var password = Resolve(ctx.GetConfig<string>("authPassword"), ctx.NodeInputs);
                var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                request.Headers.Authorization = new global::System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuth);
                break;
            case "api-key":
                var keyName = Resolve(ctx.GetConfig<string>("authApiKeyName"), ctx.NodeInputs);
                var keyValue = Resolve(ctx.GetConfig<string>("authApiKeyValue"), ctx.NodeInputs);
                var keyLocation = ctx.GetConfig<string>("authApiKeyLocation")?.ToLowerInvariant() ?? "header";
                if (keyLocation == "header")
                    request.Headers.TryAddWithoutValidation(keyName, keyValue);
                else if (keyLocation == "query")
                {
                    // Append api key as query parameter to the existing URL
                    var uriBuilder = new UriBuilder(request.RequestUri!);
                    var separator = string.IsNullOrEmpty(uriBuilder.Query) ? "?" : "&";
                    uriBuilder.Query = uriBuilder.Query.TrimStart('?') + separator + $"{Uri.EscapeDataString(keyName)}={Uri.EscapeDataString(keyValue)}";
                    request.RequestUri = uriBuilder.Uri;
                }
                break;
            case "oauth2-client-credentials":
                var tokenUrl = Resolve(ctx.GetConfig<string>("authTokenUrl"), ctx.NodeInputs);
                var clientId = Resolve(ctx.GetConfig<string>("authClientId"), ctx.NodeInputs);
                var clientSecret = Resolve(ctx.GetConfig<string>("authClientSecret"), ctx.NodeInputs);
                var scope = Resolve(ctx.GetConfig<string>("authScope"), ctx.NodeInputs);

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["scope"] = scope
                    })
                };

                var tokenResponse = await client.SendAsync(tokenRequest, ct);
                tokenResponse.EnsureSuccessStatusCode();

                var tokenJson = await tokenResponse.Content.ReadAsStringAsync(ct);
                var accessToken = JsonSerializer.Deserialize<JsonElement>(tokenJson).GetProperty("access_token").GetString();

                request.Headers.Authorization = new global::System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                break;
            default:
                throw new InvalidOperationException($"Unsupported auth type: {authType}");
        }
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
    public string Description => "Calls any external REST API endpoint with configurable auth (none, bearer, basic, api-key, OAuth2).";
    /// <inheritdoc />
    public string Category => "integrations";
    /// <inheritdoc />
    public string Version => "2.0.0";
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
        new NodeConfigDefinition("headers", "Headers", "JSON object of request headers.", DataType.String, Required: false, IsMultiline: true),
        new NodeConfigDefinition("body", "Body", "Request body for POST/PUT.", DataType.String, Required: false, IsMultiline: true),
        new NodeConfigDefinition("timeoutSeconds", "Timeout (s)", "Request timeout in seconds.", DataType.Number, Required: false, DefaultValue: 30),
        new NodeConfigDefinition("authType", "Auth Type", "Authentication type.", DataType.Enum, Required: false, DefaultValue: "none", AllowedValues: new[] { "none", "bearer", "basic", "api-key", "oauth2-client-credentials" }),
        new NodeConfigDefinition("authToken", "Auth Token", "Bearer token.", DataType.String, Required: false, IsSensitive: true),
        new NodeConfigDefinition("authUsername", "Auth Username", "Basic auth username.", DataType.String, Required: false),
        new NodeConfigDefinition("authPassword", "Auth Password", "Basic auth password.", DataType.String, Required: false, IsSensitive: true),
        new NodeConfigDefinition("authApiKeyName", "API Key Name", "API key name.", DataType.String, Required: false),
        new NodeConfigDefinition("authApiKeyValue", "API Key Value", "API key value.", DataType.String, Required: false, IsSensitive: true),
        new NodeConfigDefinition("authApiKeyLocation", "API Key Location", "Location for API key.", DataType.Enum, Required: false, DefaultValue: "header", AllowedValues: new[] { "header", "query" }),
        new NodeConfigDefinition("authTokenUrl", "Auth Token URL", "OAuth2 token URL.", DataType.String, Required: false),
        new NodeConfigDefinition("authClientId", "Client ID", "OAuth2 client ID.", DataType.String, Required: false),
        new NodeConfigDefinition("authClientSecret", "Client Secret", "OAuth2 client secret.", DataType.String, Required: false, IsSensitive: true),
        new NodeConfigDefinition("authScope", "Scope", "OAuth2 scope.", DataType.String, Required: false)
    };
}
