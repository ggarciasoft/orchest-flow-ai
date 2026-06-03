using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Helpers;
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

        var url = PlaceholderResolver.Resolve(ctx.GetConfig<string>("url") ?? throw new InvalidOperationException("url config is required"), ctx.NodeInputs);
        var method = ctx.GetConfig<string>("method")?.ToUpperInvariant() ?? "GET";
        var headersRaw = PlaceholderResolver.Resolve(ctx.GetConfig<string>("headers"), ctx.NodeInputs);
        var body = PlaceholderResolver.Resolve(ctx.GetConfig<string>("body"), ctx.NodeInputs);
        var timeoutSeconds = (int)(ctx.GetConfig<double?>("timeoutSeconds") ?? 30.0);
        var authType = ctx.GetConfig<string>("authType") ?? "none";

        // Parse headers — supports JSON object OR raw Key: Value lines
        var headers = ParseHeaders(headersRaw);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = !string.IsNullOrWhiteSpace(body) ? new StringContent(body) : null
        };
        foreach (var kv in headers)
            request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);

        // Apply authentication (with {{secret:name}} resolution)
        await ApplyAuth(request, authType, ctx, client, ct);

        var response = await client.SendAsync(request, cts.Token);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        var failOnError = ctx.GetConfig<bool?>("failOnError") ?? false;

        var outputs = new Dictionary<string, object?>
        {
            ["statusCode"] = (int)response.StatusCode,
            ["responseBody"] = responseBody,
            ["success"] = response.IsSuccessStatusCode
        };

        if (failOnError && !response.IsSuccessStatusCode)
        {
            var summary = responseBody.Length > 500 ? responseBody[..500] + "…" : responseBody;
            var errorMessage = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {summary}";
            return NodeExecutionResult.Failed(errorMessage, outputs);
        }

        return NodeExecutionResult.Succeeded(outputs);
    }

    /// <summary>
    /// Parses a headers string that can be either a JSON object or raw Key: Value lines.
    /// </summary>
    private static Dictionary<string, string> ParseHeaders(string? headersRaw)
    {
        if (string.IsNullOrWhiteSpace(headersRaw))
            return new Dictionary<string, string>();

        var trimmed = headersRaw.TrimStart();
        // Try JSON format first
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            try { return JsonSerializer.Deserialize<Dictionary<string, string>>(headersRaw) ?? new(); }
            catch (JsonException) { /* fall through to raw format */ }
        }

        // Raw format: one "Key: Value" per line
        var result = new Dictionary<string, string>();
        foreach (var line in headersRaw.Split('\n'))
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine)) continue;
            var colonIndex = trimmedLine.IndexOf(':');
            if (colonIndex <= 0) continue;
            var key = trimmedLine[..colonIndex].Trim();
            var value = trimmedLine[(colonIndex + 1)..].Trim();
            result[key] = value;
        }
        return result;
    }

    /// <summary>
    /// Applies authentication to the HTTP request based on the auth type.
    /// Resolves both {{secret:name}} and {{key}} placeholders in auth config values.
    /// </summary>
    private static async Task ApplyAuth(HttpRequestMessage request, string authType, WorkflowExecutionContext ctx, HttpClient client, CancellationToken ct)
    {
        var secretService = ctx.Services.GetService<ISecretService>();

        async Task<string> ResolveValue(string? template)
        {
            if (string.IsNullOrEmpty(template)) return template ?? string.Empty;
            // 1. Resolve {{secret:name}} → actual secret value
            if (secretService != null)
                template = (await secretService.ResolveAsync(template, ctx.TenantId, ct)) ?? template;
            // 2. Resolve {{key}} / {{key|filter}} → node input values
            return PlaceholderResolver.Resolve(template, ctx.NodeInputs);
        }

        switch (authType)
        {
            case "none":
                break;
            case "bearer":
                var token = await ResolveValue(ctx.GetConfig<string>("authToken"));
                request.Headers.Authorization = new global::System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                break;
            case "basic":
                var username = await ResolveValue(ctx.GetConfig<string>("authUsername"));
                var password = await ResolveValue(ctx.GetConfig<string>("authPassword"));
                var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                request.Headers.Authorization = new global::System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuth);
                break;
            case "api-key":
                var keyName = await ResolveValue(ctx.GetConfig<string>("authApiKeyName"));
                var keyValue = await ResolveValue(ctx.GetConfig<string>("authApiKeyValue"));
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
                var tokenUrl = await ResolveValue(ctx.GetConfig<string>("authTokenUrl"));
                var clientId = await ResolveValue(ctx.GetConfig<string>("authClientId"));
                var clientSecret = await ResolveValue(ctx.GetConfig<string>("authClientSecret"));
                var scope = await ResolveValue(ctx.GetConfig<string>("authScope"));

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
    public string Version => "2.1.0";
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
        new NodeConfigDefinition("url", "URL", "The URL to call. Supports {{key}} and {{secret:name}}.", DataType.String, Required: true),
        new NodeConfigDefinition("method", "Method", "HTTP method.", DataType.Enum, Required: false, DefaultValue: "GET", AllowedValues: new[] { "GET", "POST", "PUT", "DELETE", "PATCH" }),
        new NodeConfigDefinition("headers", "Headers", "JSON object or Key: Value lines of request headers.", DataType.String, Required: false, IsMultiline: true),
        new NodeConfigDefinition("body", "Body", "Request body for POST/PUT. Supports {{key}} placeholders.", DataType.String, Required: false, IsMultiline: true),
        new NodeConfigDefinition("timeoutSeconds", "Timeout (s)", "Request timeout in seconds.", DataType.Number, Required: false, DefaultValue: 30),
        new NodeConfigDefinition("failOnError", "Fail on Error", "When enabled, the step will be marked as failed if the HTTP response is not successful (non-2xx).", DataType.Boolean, Required: false, DefaultValue: false),
        new NodeConfigDefinition("authType", "Auth Type", "Authentication type.", DataType.Enum, Required: false, DefaultValue: "none", AllowedValues: new[] { "none", "bearer", "basic", "api-key", "oauth2-client-credentials" }),
        new NodeConfigDefinition("authToken", "Auth Token", "Bearer token. Supports {{secret:name}} and {{key}}.", DataType.String, Required: false, IsSensitive: true),
        new NodeConfigDefinition("authUsername", "Auth Username", "Basic auth username. Supports {{secret:name}} and {{key}}.", DataType.String, Required: false, IsSensitive: true),
        new NodeConfigDefinition("authPassword", "Auth Password", "Basic auth password. Supports {{secret:name}} and {{key}}.", DataType.String, Required: false, IsSensitive: true),
        new NodeConfigDefinition("authApiKeyName", "API Key Name", "API key name. Supports {{key}}.", DataType.String, Required: false),
        new NodeConfigDefinition("authApiKeyValue", "API Key Value", "API key value. Supports {{secret:name}} and {{key}}.", DataType.String, Required: false, IsSensitive: true),
        new NodeConfigDefinition("authApiKeyLocation", "API Key Location", "Location for API key.", DataType.Enum, Required: false, DefaultValue: "header", AllowedValues: new[] { "header", "query" }),
        new NodeConfigDefinition("authTokenUrl", "Auth Token URL", "OAuth2 token URL. Supports {{key}}.", DataType.String, Required: false),
        new NodeConfigDefinition("authClientId", "Client ID", "OAuth2 client ID. Supports {{secret:name}} and {{key}}.", DataType.String, Required: false),
        new NodeConfigDefinition("authClientSecret", "Client Secret", "OAuth2 client secret. Supports {{secret:name}} and {{key}}.", DataType.String, Required: false, IsSensitive: true),
        new NodeConfigDefinition("authScope", "Scope", "OAuth2 scope. Supports {{key}}.", DataType.String, Required: false)
    };
}
