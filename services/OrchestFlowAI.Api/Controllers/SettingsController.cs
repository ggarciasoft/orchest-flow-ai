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
    private readonly IHttpClientFactory _httpClientFactory;

    public SettingsController(IPlatformSettingsService settings, OpenAIApiKeyHolder openAiKeyHolder, IHttpClientFactory httpClientFactory)
    { _settings = settings; _openAiKeyHolder = openAiKeyHolder; _httpClientFactory = httpClientFactory; }

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
            result[kv.Key] = (kv.Key.Contains("key", StringComparison.OrdinalIgnoreCase) ||
                              kv.Key.Contains("secret", StringComparison.OrdinalIgnoreCase))
                ? MaskKey(kv.Value)
                : kv.Value;
        }

        // Always include known settings even if not yet set
        result.TryAdd("llm.openai.apiKey", null);
        result.TryAdd("llm.defaultModel", all.GetValueOrDefault("llm.defaultModel") ?? "gpt-4o-mini");
        result.TryAdd("llm.defaultProvider", all.GetValueOrDefault("llm.defaultProvider") ?? "openai");
        result.TryAdd("llm.anthropic.apiKey", null);
        result.TryAdd("llm.azure.endpoint", all.GetValueOrDefault("llm.azure.endpoint") ?? "");
        result.TryAdd("llm.azure.apiKey", null);
        result.TryAdd("llm.azure.deploymentName", all.GetValueOrDefault("llm.azure.deploymentName") ?? "");
        result.TryAdd("llm.ollama.baseUrl", all.GetValueOrDefault("llm.ollama.baseUrl") ?? "http://localhost:11434");

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
        var tenantId = GetTenantId();
        // Prefer the in-memory holder (hot-reloaded on save); fall back to DB
        // for keys that were stored before this process started.
        var key = _openAiKeyHolder.ApiKey;
        if (string.IsNullOrWhiteSpace(key))
            key = await _settings.GetAsync(tenantId, "llm.openai.apiKey", ct);

        if (string.IsNullOrWhiteSpace(key))
            return Ok(new { success = false, message = "No API key configured." });

        // Keep the holder warm for future calls
        _openAiKeyHolder.Update(key);

        try
        {
            var client = _httpClientFactory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
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

    /// <summary>Tests the Anthropic connection with the stored API key.</summary>
    [HttpPost("test/anthropic")]
    public async Task<IActionResult> TestAnthropic(CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var key = await _settings.GetAsync(tenantId, "llm.anthropic.apiKey", ct);
        if (string.IsNullOrWhiteSpace(key))
            return Ok(new { success = false, message = "No Anthropic API key configured." });

        try
        {
            var client = _httpClientFactory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            req.Headers.Add("x-api-key", key);
            req.Headers.Add("anthropic-version", "2023-06-01");
            req.Content = System.Net.Http.Json.JsonContent.Create(new
            {
                model = "claude-3-haiku-20240307",
                max_tokens = 16,
                messages = new[] { new { role = "user", content = "Hi" } }
            });

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

    /// <summary>Tests the Azure OpenAI connection with stored settings.</summary>
    [HttpPost("test/azure")]
    public async Task<IActionResult> TestAzure(CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var endpoint = await _settings.GetAsync(tenantId, "llm.azure.endpoint", ct);
        var apiKey = await _settings.GetAsync(tenantId, "llm.azure.apiKey", ct);
        var deploymentName = await _settings.GetAsync(tenantId, "llm.azure.deploymentName", ct);

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(deploymentName))
            return Ok(new { success = false, message = "Endpoint, API key, and deployment name are all required." });

        try
        {
            var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{deploymentName}/chat/completions?api-version=2024-02-01";
            var client = _httpClientFactory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("api-key", apiKey);
            req.Content = System.Net.Http.Json.JsonContent.Create(new
            {
                messages = new[] { new { role = "user", content = "Hi" } },
                max_tokens = 16
            });

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

    /// <summary>Tests the Ollama connection with stored base URL.</summary>
    [HttpPost("test/ollama")]
    public async Task<IActionResult> TestOllama(CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var baseUrl = await _settings.GetAsync(tenantId, "llm.ollama.baseUrl", ct)
                      ?? "http://localhost:11434";

        try
        {
            var client = _httpClientFactory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/generate");
            req.Content = System.Net.Http.Json.JsonContent.Create(new
            {
                model = "llama3",
                prompt = "Hi",
                stream = false
            });

            var resp = await client.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode)
                return Ok(new { success = true, message = "Ollama connection successful." });
            var body = await resp.Content.ReadAsStringAsync(ct);
            return Ok(new { success = false, message = $"Ollama returned {(int)resp.StatusCode}: {body[..Math.Min(200, body.Length)]}" });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = $"Could not connect to Ollama: {ex.Message}" });
        }
    }

    /// <summary>Returns which AI providers are ready to use (have a configured API key).</summary>
    [HttpGet("ai-status")]
    public async Task<IActionResult> AiStatus(CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var openAiKey   = await _settings.GetAsync(tenantId, "llm.openai.apiKey",       ct) ?? _openAiKeyHolder.ApiKey;
        var anthropicKey = await _settings.GetAsync(tenantId, "llm.anthropic.apiKey",   ct);
        var azureKey     = await _settings.GetAsync(tenantId, "llm.azure.apiKey",       ct);
        var azureEndpoint = await _settings.GetAsync(tenantId, "llm.azure.endpoint",    ct);
        var ollamaUrl    = await _settings.GetAsync(tenantId, "llm.ollama.baseUrl",     ct);
        var defaultProvider = await _settings.GetAsync(tenantId, "llm.defaultProvider", ct) ?? "openai";
        var defaultModel    = await _settings.GetAsync(tenantId, "llm.defaultModel",    ct) ?? "gpt-4o-mini";

        var configured = new Dictionary<string, bool>
        {
            ["openai"]    = !string.IsNullOrWhiteSpace(openAiKey),
            ["anthropic"] = !string.IsNullOrWhiteSpace(anthropicKey),
            ["azure"]     = !string.IsNullOrWhiteSpace(azureKey) && !string.IsNullOrWhiteSpace(azureEndpoint),
            ["ollama"]    = true,
        };

        return Ok(new
        {
            defaultProvider,
            defaultModel,
            isDefaultConfigured = configured.GetValueOrDefault(defaultProvider, false),
            providers = configured,
        });
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
