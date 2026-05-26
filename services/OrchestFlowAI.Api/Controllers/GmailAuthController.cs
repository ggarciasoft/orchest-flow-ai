using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Api.Controllers;

[ApiController, Route("api/gmail")]
public sealed class GmailAuthController : ControllerBase
{
    private readonly IGmailCredentialRepository _repo;
    private readonly IHttpClientFactory _http;

    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public GmailAuthController(IGmailCredentialRepository repo, IHttpClientFactory http)
    { _repo = repo; _http = http; }

    /// <summary>
    /// Starts the OAuth2 flow. Redirects to Google consent screen.
    /// Requires authentication so we can embed tenantId in state.
    /// </summary>
    [HttpGet("auth/start"), Authorize]
    public IActionResult Start(
        [FromQuery] string name,
        [FromQuery] string clientId,
        [FromQuery] string clientSecret,
        [FromQuery] string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            return BadRequest(new { detail = "name, clientId and clientSecret are required" });

        var tenantId = User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString();

        // Encode all context needed for the callback in state (base64 JSON)
        var stateObj = new { tenantId, name, clientId, clientSecret };
        var state = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(stateObj)));

        // Use the first registered redirect URI if not specified
        var callbackUri = redirectUri ?? $"{Request.Scheme}://{Request.Host}/api/gmail/callback";

        var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(callbackUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString("https://www.googleapis.com/auth/gmail.readonly")}" +
            $"&access_type=offline" +
            $"&prompt=consent" +
            $"&state={Uri.EscapeDataString(state)}";

        return Redirect(authUrl);
    }

    /// <summary>
    /// OAuth2 callback from Google. Exchanges auth code for tokens and stores credential.
    /// </summary>
    [HttpGet("callback"), AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        [FromQuery] string? error,
        CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(error))
            return Content(HtmlPage("Authorization failed", $"<p style='color:red'>Google returned: {error}</p>"), "text/html");

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Content(HtmlPage("Bad Request", "<p>Missing code or state parameter.</p>"), "text/html");

        // Decode state
        StatePayload? stateData;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(state));
            stateData = JsonSerializer.Deserialize<StatePayload>(json, _jsonOpts);
        }
        catch
        {
            return Content(HtmlPage("Bad Request", "<p>Invalid state parameter.</p>"), "text/html");
        }

        if (stateData == null || string.IsNullOrEmpty(stateData.ClientId))
            return Content(HtmlPage("Bad Request", "<p>Incomplete state payload.</p>"), "text/html");

        // Determine the redirect_uri to pass in token exchange (must match what was used in auth/start)
        var callbackUri = $"{Request.Scheme}://{Request.Host}/api/gmail/callback";

        // Exchange auth code for tokens
        var client = _http.CreateClient();
        var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = stateData.ClientId,
                ["client_secret"] = stateData.ClientSecret,
                ["redirect_uri"] = callbackUri,
                ["grant_type"] = "authorization_code",
            })
        };
        var tokenResp = await client.SendAsync(tokenReq, ct);
        var tokenBody = await tokenResp.Content.ReadAsStringAsync(ct);

        if (!tokenResp.IsSuccessStatusCode)
            return Content(HtmlPage("Token Exchange Failed", $"<pre>{System.Net.WebUtility.HtmlEncode(tokenBody)}</pre>"), "text/html");

        var tokenDoc = JsonSerializer.Deserialize<JsonElement>(tokenBody);
        var refreshToken = tokenDoc.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var accessToken = tokenDoc.TryGetProperty("access_token", out var at) ? at.GetString() : null;

        if (string.IsNullOrEmpty(refreshToken))
            return Content(HtmlPage("No Refresh Token",
                "<p>Google did not return a refresh_token. Make sure you used <b>prompt=consent</b> and <b>access_type=offline</b>.</p>"), "text/html");

        // Fetch Gmail address
        string? email = null;
        if (!string.IsNullOrEmpty(accessToken))
        {
            var profileReq = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/gmail/v1/users/me/profile");
            profileReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var profileResp = await client.SendAsync(profileReq, ct);
            if (profileResp.IsSuccessStatusCode)
            {
                var profileBody = await profileResp.Content.ReadAsStringAsync(ct);
                var profileDoc = JsonSerializer.Deserialize<JsonElement>(profileBody);
                email = profileDoc.TryGetProperty("emailAddress", out var ea) ? ea.GetString() : null;
            }
        }

        // Upsert credential
        if (!Guid.TryParse(stateData.TenantId, out var tenantId))
            tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // fallback to dev tenant

        var existing = await _repo.GetByNameAsync(stateData.Name, tenantId, ct);
        if (existing != null)
        {
            existing.UpdateTokens(refreshToken, email);
            await _repo.UpdateAsync(existing, ct);
        }
        else
        {
            var cred = GmailCredential.Create(tenantId, stateData.Name, stateData.ClientId, stateData.ClientSecret, refreshToken, email);
            await _repo.CreateAsync(cred, ct);
        }

        var displayEmail = email ?? "(unknown)";
        return Content(HtmlPage("Connected!",
            $"<p>✅ Gmail account <b>{System.Net.WebUtility.HtmlEncode(displayEmail)}</b> connected as <b>{System.Net.WebUtility.HtmlEncode(stateData.Name)}</b>.</p>" +
            $"<p>You can now use <code>credentialName: \"{System.Net.WebUtility.HtmlEncode(stateData.Name)}\"</code> in the GmailReadNode config.</p>" +
            $"<p><a href='/workflows'>← Back to workflows</a></p>"), "text/html");
    }

    /// <summary>Lists all Gmail credentials for the authenticated tenant (names + emails only, no secrets).</summary>
    [HttpGet("credentials"), Authorize]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var creds = await _repo.ListAsync(tenantId, ct);
        return Ok(creds.Select(c => new { c.Id, c.Name, c.Email, c.CreatedAt, c.UpdatedAt }));
    }

    /// <summary>Deletes a Gmail credential by id.</summary>
    [HttpDelete("credentials/{id:guid}"), Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        await _repo.DeleteAsync(id, tenantId, ct);
        return NoContent();
    }

    // ---- helpers ----

    private Guid GetTenantId()
    {
        var raw = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(raw, out var g) ? g : Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private static string HtmlPage(string title, string body) => $$$"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>{{{title}}} — OrchestFlowAI</title>
        <style>body{font-family:system-ui,sans-serif;max-width:600px;margin:60px auto;padding:0 20px}
        h1{color:#1e293b}code{background:#f1f5f9;padding:2px 6px;border-radius:4px}</style>
        </head>
        <body><h1>{{{title}}}</h1>{{{body}}}</body>
        </html>
        """;

    private sealed class StatePayload
    {
        public string TenantId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string ClientId { get; set; } = default!;
        public string ClientSecret { get; set; } = default!;
    }
}
