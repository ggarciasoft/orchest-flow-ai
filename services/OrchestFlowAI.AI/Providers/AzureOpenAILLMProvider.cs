using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.Application.Abstractions;

namespace OrchestFlowAI.AI.Providers;

public sealed class AzureOpenAILLMProvider : ILLMProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPlatformSettingsService? _platformSettings;
    private readonly ILogger<AzureOpenAILLMProvider> _logger;

    public string Id => "azure";
    public IReadOnlyCollection<string> Models => _models;
    private string[] _models = Array.Empty<string>();

    public AzureOpenAILLMProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<AzureOpenAILLMProvider> logger,
        IPlatformSettingsService? platformSettings = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _platformSettings = platformSettings;
    }

    private async Task<(string Endpoint, string ApiKey, string DeploymentName)> ResolveSettingsAsync(Guid? tenantId, CancellationToken ct)
    {
        string? endpoint = null, apiKey = null, deploymentName = null;
        if (_platformSettings != null && tenantId.HasValue)
        {
            endpoint = await _platformSettings.GetAsync(tenantId.Value, "llm.azure.endpoint", ct);
            apiKey = await _platformSettings.GetAsync(tenantId.Value, "llm.azure.apiKey", ct);
            deploymentName = await _platformSettings.GetAsync(tenantId.Value, "llm.azure.deploymentName", ct);
        }
        endpoint ??= Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "";
        apiKey ??= Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "";
        deploymentName ??= Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-4o";
        return (endpoint.TrimEnd('/'), apiKey, deploymentName);
    }

    public async Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken ct = default)
    {
        var (endpoint, apiKey, deploymentName) = await ResolveSettingsAsync(request.TenantId, ct);

        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("Azure OpenAI endpoint is not configured.");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Azure OpenAI API key is not configured.");

        // Use deployment name as model identifier (or override from request)
        var model = request.Model == "default" ? deploymentName : request.Model;
        // Update models list dynamically
        _models = new[] { deploymentName };

        var url = $"{endpoint}/openai/deployments/{model}/chat/completions?api-version=2024-02-01";

        var messages = new List<object>();
        if (request.SystemPrompt != null)
            messages.Add(new { role = "system", content = request.SystemPrompt });
        messages.Add(new { role = "user", content = request.Prompt });

        var body = new
        {
            messages,
            max_tokens = request.MaxTokens ?? 1024,
            temperature = request.Temperature ?? 0.7
        };

        var client = _httpClientFactory.CreateClient("azure-openai");
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("api-key", apiKey);
        req.Content = JsonContent.Create(body);

        var resp = await client.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("Azure OpenAI error {Status}: {Body}", (int)resp.StatusCode, raw);
            throw new HttpRequestException($"Azure OpenAI returned {(int)resp.StatusCode}: {raw[..Math.Min(300, raw.Length)]}");
        }

        using var doc = JsonDocument.Parse(raw);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        var usage = doc.RootElement.TryGetProperty("usage", out var u)
            ? new LLMUsage(
                u.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0,
                u.TryGetProperty("completion_tokens", out var ct2) ? ct2.GetInt32() : 0,
                u.TryGetProperty("total_tokens", out var tt) ? tt.GetInt32() : 0)
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
