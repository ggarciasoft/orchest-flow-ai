using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.Application.Abstractions;

namespace OrchestFlowAI.AI.Providers;

public sealed class AnthropicLLMProvider : ILLMProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPlatformSettingsService? _platformSettings;
    private readonly ISecretService? _secretService;
    private readonly ILogger<AnthropicLLMProvider> _logger;

    public string Id => "anthropic";
    public IReadOnlyCollection<string> Models => new[]
    {
        "claude-3-5-sonnet-20241022",
        "claude-3-haiku-20240307",
        "claude-3-opus-20240229"
    };

    public AnthropicLLMProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<AnthropicLLMProvider> logger,
        IPlatformSettingsService? platformSettings = null,
        ISecretService? secretService = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _platformSettings = platformSettings;
        _secretService = secretService;
    }

    private async Task<string> ResolveApiKeyAsync(Guid? tenantId, CancellationToken ct)
    {
        if (_platformSettings != null && tenantId.HasValue)
        {
            var dbKey = await _platformSettings.GetAsync(tenantId.Value, "llm.anthropic.apiKey", ct);
            if (!string.IsNullOrWhiteSpace(dbKey))
                return await _secretService?.ResolveAsync(dbKey, tenantId.Value, ct) ?? dbKey;
        }
        return Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? "";
    }

    public async Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken ct = default)
    {
        var model = request.Model == "default" ? "claude-3-5-sonnet-20241022" : request.Model;
        var apiKey = await ResolveApiKeyAsync(request.TenantId, ct);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Anthropic API key is not configured.");

        var client = _httpClientFactory.CreateClient("anthropic");
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");

        var messages = new List<object> { new { role = "user", content = request.Prompt } };
        var systemPrompt = request.SystemPrompt;
        var body = new
        {
            model,
            max_tokens = request.MaxTokens ?? 1024,
            messages,
            system = systemPrompt
        };

        req.Content = JsonContent.Create(body);
        var resp = await client.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("Anthropic API error {Status}: {Body}", (int)resp.StatusCode, raw);
            throw new HttpRequestException($"Anthropic API returned {(int)resp.StatusCode}: {raw[..Math.Min(300, raw.Length)]}");
        }

        using var doc = JsonDocument.Parse(raw);
        var text = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? "";

        var usage = doc.RootElement.TryGetProperty("usage", out var u)
            ? new LLMUsage(
                u.TryGetProperty("input_tokens", out var inp) ? inp.GetInt32() : 0,
                u.TryGetProperty("output_tokens", out var out_) ? out_.GetInt32() : 0,
                (u.TryGetProperty("input_tokens", out var i2) ? i2.GetInt32() : 0) +
                (u.TryGetProperty("output_tokens", out var o2) ? o2.GetInt32() : 0))
            : new LLMUsage(0, 0, 0);

        return new LLMResponse(text, usage);
    }

    public async Task<LLMResponse<TOutput>> GenerateStructuredAsync<TOutput>(LLMRequest request, string jsonSchema, CancellationToken ct = default)
    {
        var augmented = request with
        {
            SystemPrompt = (request.SystemPrompt ?? "") +
                           "\n\nReturn ONLY valid JSON matching this schema: " + jsonSchema
        };
        var textResponse = await GenerateTextAsync(augmented, ct);
        var raw = textResponse.Text.Trim();

        if (raw.StartsWith("```"))
        {
            var firstNewline = raw.IndexOf('\n');
            var lastFence = raw.LastIndexOf("```");
            if (firstNewline >= 0 && lastFence > firstNewline)
                raw = raw[(firstNewline + 1)..lastFence].Trim();
        }

        var output = JsonSerializer.Deserialize<TOutput>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Failed to deserialize structured AI output");
        return new LLMResponse<TOutput>(output, raw, textResponse.Usage);
    }
}
