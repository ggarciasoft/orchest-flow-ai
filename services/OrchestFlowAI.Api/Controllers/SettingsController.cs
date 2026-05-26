using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.AI.Providers;
using OrchestFlowAI.Application.Abstractions;

namespace OrchestFlowAI.Api.Controllers;

[ApiController, Route("api/settings"), Authorize]
public sealed class SettingsController : ControllerBase
{
    private readonly IPlatformSettingsService _settings;
    private readonly OpenAIApiKeyHolder _openAiKeyHolder;

    public SettingsController(IPlatformSettingsService settings, OpenAIApiKeyHolder openAiKeyHolder)
    { _settings = settings; _openAiKeyHolder = openAiKeyHolder; }

    /// <summary>Returns current platform settings. API keys are masked.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var all = await _settings.GetAllAsync(tenantId, ct);

        // Mask sensitive values
        var result = new Dictionary<string, string?>();
        foreach (var kv in all)
        {
            result[kv.Key] = kv.Key.Contains("key", StringComparison.OrdinalIgnoreCase)
                ? MaskKey(kv.Value)
                : kv.Value;
        }

        // Always include known settings even if not yet set
        result.TryAdd("llm.openai.apiKey", null);
        result.TryAdd("llm.defaultModel", all.GetValueOrDefault("llm.defaultModel") ?? "gpt-4o-mini");
        result.TryAdd("llm.defaultProvider", all.GetValueOrDefault("llm.defaultProvider") ?? "openai");

        return Ok(result);
    }

    /// <summary>Updates platform settings. Set apiKey to empty string to keep existing.</summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Dictionary<string, string> updates, CancellationToken ct)
    {
        var tenantId = GetTenantId();

        foreach (var kv in updates)
        {
            if (string.IsNullOrEmpty(kv.Value)) continue;

            await _settings.SetAsync(tenantId, kv.Key, kv.Value, ct);

            // Hot-reload OpenAI key immediately
            if (kv.Key == "llm.openai.apiKey")
                _openAiKeyHolder.Update(kv.Value);
        }

        return NoContent();
    }

    /// <summary>Tests the OpenAI connection with the current API key.</summary>
    [HttpPost("test/openai")]
    public async Task<IActionResult> TestOpenAI(CancellationToken ct)
    {
        var key = _openAiKeyHolder.ApiKey;
        if (string.IsNullOrWhiteSpace(key))
            return Ok(new { success = false, message = "No API key configured." });

        try
        {
            var client = new System.Net.Http.HttpClient();
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get,
                "https://api.openai.com/v1/models");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
            var resp = await client.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode)
                return Ok(new { success = true, message = "Connection successful." });
            var body = await resp.Content.ReadAsStringAsync(ct);
            return Ok(new { success = false, message = $"API returned {(int)resp.StatusCode}: {body[..Math.Min(200, body.Length)]}" });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    private Guid GetTenantId()
    {
        var raw = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(raw, out var g) ? g : Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private static string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (key.Length <= 8) return new string('*', key.Length);
        return key[..4] + new string('*', key.Length - 8) + key[^4..];
    }
}
