using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.Application.Abstractions;

namespace OrchestFlowAI.AI.Providers;

public sealed class OllamaLLMProvider : ILLMProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPlatformSettingsService? _platformSettings;
    private readonly ILogger<OllamaLLMProvider> _logger;

    public string Id => "ollama";
    public IReadOnlyCollection<string> Models => new[] { "llama3", "mistral", "phi3", "gemma2" };

    public OllamaLLMProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<OllamaLLMProvider> logger,
        IPlatformSettingsService? platformSettings = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _platformSettings = platformSettings;
    }

    private async Task<string> ResolveBaseUrlAsync(Guid? tenantId, CancellationToken ct)
    {
        if (_platformSettings != null && tenantId.HasValue)
        {
            var dbUrl = await _platformSettings.GetAsync(tenantId.Value, "llm.ollama.baseUrl", ct);
            if (!string.IsNullOrWhiteSpace(dbUrl)) return dbUrl.TrimEnd('/');
        }
        return "http://localhost:11434";
    }

    public async Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken ct = default)
    {
        var model = request.Model == "default" ? "llama3" : request.Model;
        var baseUrl = await ResolveBaseUrlAsync(request.TenantId, ct);

        var client = _httpClientFactory.CreateClient("ollama");
        var prompt = request.SystemPrompt != null
            ? $"System: {request.SystemPrompt}\n\nUser: {request.Prompt}"
            : request.Prompt;

        var body = new { model, prompt, stream = false };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/generate");
        req.Content = JsonContent.Create(body);

        var resp = await client.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("Ollama API error {Status}: {Body}", (int)resp.StatusCode, raw);
            throw new HttpRequestException($"Ollama API returned {(int)resp.StatusCode}: {raw[..Math.Min(300, raw.Length)]}");
        }

        using var doc = JsonDocument.Parse(raw);
        var text = doc.RootElement.TryGetProperty("response", out var r) ? r.GetString() ?? "" : "";

        return new LLMResponse(text, new LLMUsage(0, 0, 0));
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
