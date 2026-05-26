using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.Integrations;

/// <summary>
/// Reads emails from Gmail via the Gmail REST API using the OAuth2 refresh token flow.
/// Fetches messages matching a query, then retrieves full message details including
/// subject, sender, date, and decoded body.
/// </summary>
public sealed class GmailReadNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "integrations.gmail.read";

    /// <inheritdoc />
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var clientId = ctx.GetConfig<string>("clientId");
        var clientSecret = ctx.GetConfig<string>("clientSecret");
        var refreshToken = ctx.GetConfig<string>("refreshToken");

        var credentialName = ctx.GetConfig<string>("credentialName");
        if (!string.IsNullOrEmpty(credentialName))
        {
            var credRepo = ctx.Services.GetRequiredService<IGmailCredentialRepository>();
            var cred = await credRepo.GetByNameAsync(credentialName, ctx.TenantId, ct)
                ?? throw new InvalidOperationException($"Gmail credential '{credentialName}' not found");
            clientId = cred.ClientId;
            clientSecret = cred.ClientSecret;
            refreshToken = cred.RefreshToken;
        }

        if (string.IsNullOrEmpty(clientId)) throw new InvalidOperationException("clientId config is required");
        if (string.IsNullOrEmpty(clientSecret)) throw new InvalidOperationException("clientSecret config is required");
        if (string.IsNullOrEmpty(refreshToken)) throw new InvalidOperationException("refreshToken config is required");
        var query = ctx.GetConfig<string>("query") ?? "is:unread";
        var maxResults = (int)(ctx.GetConfig<double?>("maxResults") ?? 10.0);
        if (maxResults < 1) maxResults = 1;
        if (maxResults > 50) maxResults = 50;

        var client = ctx.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

        // Step 1: Exchange refresh token for access token
        var accessToken = await GetAccessTokenAsync(client, clientId, clientSecret, refreshToken, ct);

        // Step 2: List messages matching the query
        var listUrl = $"https://gmail.googleapis.com/gmail/v1/users/me/messages?q={Uri.EscapeDataString(query)}&maxResults={maxResults}";
        var listRequest = new HttpRequestMessage(HttpMethod.Get, listUrl);
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var listResponse = await client.SendAsync(listRequest, ct);
        listResponse.EnsureSuccessStatusCode();
        var listJson = await listResponse.Content.ReadAsStringAsync(ct);
        var listDoc = JsonDocument.Parse(listJson);

        var emails = new List<object>();

        if (!listDoc.RootElement.TryGetProperty("messages", out var messagesEl) || messagesEl.ValueKind != JsonValueKind.Array)
        {
            return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
            {
                ["emails"] = "[]",
                ["count"] = 0
            });
        }

        // Step 3: Fetch each message
        foreach (var msgRef in messagesEl.EnumerateArray())
        {
            if (ct.IsCancellationRequested) break;

            var msgId = msgRef.GetProperty("id").GetString()!;
            var threadId = msgRef.TryGetProperty("threadId", out var tid) ? tid.GetString() : null;

            var msgUrl = $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{msgId}?format=full";
            var msgRequest = new HttpRequestMessage(HttpMethod.Get, msgUrl);
            msgRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var msgResponse = await client.SendAsync(msgRequest, ct);
            if (!msgResponse.IsSuccessStatusCode) continue;

            var msgJson = await msgResponse.Content.ReadAsStringAsync(ct);
            var msgDoc = JsonDocument.Parse(msgJson);
            var root = msgDoc.RootElement;

            // Parse headers
            string subject = "", from = "", date = "";
            if (root.TryGetProperty("payload", out var payload) &&
                payload.TryGetProperty("headers", out var headers) &&
                headers.ValueKind == JsonValueKind.Array)
            {
                foreach (var h in headers.EnumerateArray())
                {
                    var name = h.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var val = h.TryGetProperty("value", out var vv) ? vv.GetString() ?? "" : "";
                    if (name.Equals("Subject", StringComparison.OrdinalIgnoreCase)) subject = val;
                    else if (name.Equals("From", StringComparison.OrdinalIgnoreCase)) from = val;
                    else if (name.Equals("Date", StringComparison.OrdinalIgnoreCase)) date = val;
                }
            }

            var snippet = root.TryGetProperty("snippet", out var snip) ? snip.GetString() ?? "" : "";
            var body = ExtractBody(root);

            emails.Add(new
            {
                id = msgId,
                threadId,
                subject,
                from,
                date,
                body,
                snippet
            });
        }

        return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
        {
            ["emails"] = JsonSerializer.Serialize(emails),
            ["count"] = emails.Count
        });
    }

    private static async Task<string> GetAccessTokenAsync(HttpClient client, string clientId, string clientSecret, string refreshToken, CancellationToken ct)
    {
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken
            })
        };

        var response = await client.SendAsync(tokenRequest, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("No access_token in OAuth2 response");
    }

    /// <summary>
    /// Extracts the text/plain or text/html body from a Gmail message payload,
    /// handling both single-part and multi-part messages. Decodes base64url.
    /// </summary>
    private static string ExtractBody(JsonElement root)
    {
        if (!root.TryGetProperty("payload", out var payload))
            return "";

        // Single-part: try payload.body.data
        if (payload.TryGetProperty("body", out var body) &&
            body.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.String)
        {
            var raw = data.GetString();
            if (!string.IsNullOrEmpty(raw))
                return DecodeBase64Url(raw);
        }

        // Multi-part: traverse parts
        if (payload.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array)
        {
            var plain = FindPartBody(parts, "text/plain");
            if (!string.IsNullOrEmpty(plain)) return plain;
            var html = FindPartBody(parts, "text/html");
            if (!string.IsNullOrEmpty(html)) return html;
        }

        return "";
    }

    private static string FindPartBody(JsonElement parts, string mimeType)
    {
        foreach (var part in parts.EnumerateArray())
        {
            var mime = part.TryGetProperty("mimeType", out var mt) ? mt.GetString() ?? "" : "";
            if (mime.Equals(mimeType, StringComparison.OrdinalIgnoreCase))
            {
                if (part.TryGetProperty("body", out var b) &&
                    b.TryGetProperty("data", out var d) &&
                    d.ValueKind == JsonValueKind.String)
                {
                    var raw = d.GetString();
                    if (!string.IsNullOrEmpty(raw)) return DecodeBase64Url(raw);
                }
            }

            // Recurse into nested parts
            if (part.TryGetProperty("parts", out var nested) && nested.ValueKind == JsonValueKind.Array)
            {
                var result = FindPartBody(nested, mimeType);
                if (!string.IsNullOrEmpty(result)) return result;
            }
        }
        return "";
    }

    private static string DecodeBase64Url(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        // Pad to multiple of 4
        var padding = base64.Length % 4;
        if (padding != 0)
            base64 += new string('=', 4 - padding);
        var bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }
}

/// <summary>Descriptor for <see cref="GmailReadNode"/>.</summary>
public sealed class GmailReadNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "integrations.gmail.read";
    /// <inheritdoc />
    public string DisplayName => "Gmail Read";
    /// <inheritdoc />
    public string Description => "Reads emails from Gmail using OAuth2 refresh token. Returns a structured array of emails with subject, sender, date, and body.";
    /// <inheritdoc />
    public string Category => "integrations";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "mail";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("emails", "Emails", "JSON array of email objects: [{id, threadId, subject, from, date, body, snippet}].", DataType.String),
        new NodeOutputDefinition("count", "Count", "Number of emails returned.", DataType.Number)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("credentialName", "Credential Name", "Name of a saved Gmail credential (from /api/gmail/auth/start). If set, clientId/clientSecret/refreshToken are not needed.", DataType.String, Required: false),
        new NodeConfigDefinition("clientId", "Client ID", "OAuth2 client ID from Google Cloud Console.", DataType.String, Required: false),
        new NodeConfigDefinition("clientSecret", "Client Secret", "OAuth2 client secret from Google Cloud Console.", DataType.String, Required: false),
        new NodeConfigDefinition("refreshToken", "Refresh Token", "OAuth2 refresh token with Gmail read scope.", DataType.String, Required: false),
        new NodeConfigDefinition("query", "Query", "Gmail search query (e.g. is:unread, from:someone@example.com).", DataType.String, Required: false, DefaultValue: "is:unread"),
        new NodeConfigDefinition("maxResults", "Max Results", "Maximum emails to retrieve (1-50, default: 10).", DataType.Number, Required: false, DefaultValue: 10)
    };
}
