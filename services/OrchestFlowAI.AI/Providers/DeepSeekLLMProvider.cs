using System.Text.Json;
using OpenAI.Chat;
using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace OrchestFlowAI.AI.Providers;

/// <summary>
/// DeepSeek LLM provider — uses DeepSeek's OpenAI-compatible chat completions API.
/// Endpoint: https://api.deepseek.com
/// Docs: https://platform.deepseek.com/docs
/// </summary>
public sealed class DeepSeekLLMProvider : ILLMProvider
{
    private readonly IPlatformSettingsService? _platformSettings;
    private readonly ISecretService? _secretService;
    private readonly ILogger<DeepSeekLLMProvider> _logger;

    public string Id => "deepseek";
    public IReadOnlyCollection<string> Models => new[] { "deepseek-chat", "deepseek-reasoner" };

    public DeepSeekLLMProvider(
        ILogger<DeepSeekLLMProvider> logger,
        IPlatformSettingsService? platformSettings = null,
        ISecretService? secretService = null)
    {
        _logger = logger;
        _platformSettings = platformSettings;
        _secretService = secretService;
    }

    private async Task<string> ResolveApiKeyAsync(Guid? tenantId, CancellationToken ct)
    {
        if (_platformSettings != null && tenantId.HasValue)
        {
            var dbKey = await _platformSettings.GetAsync(tenantId.Value, "llm.deepseek.apiKey", ct);
            if (!string.IsNullOrWhiteSpace(dbKey))
            {
                var resolved = _secretService != null
                    ? await _secretService.ResolveAsync(dbKey, tenantId.Value, ct) : dbKey;
                return resolved ?? dbKey;
            }
        }
        return Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") ?? "";
    }

    public async Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken ct = default)
    {
        var model = request.Model == "default" ? "deepseek-chat" : request.Model;
        var apiKey = await ResolveApiKeyAsync(request.TenantId, ct);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("DeepSeek API key is not configured. Go to Settings → AI Providers.");

        // DeepSeek is OpenAI-compatible — use the OpenAI SDK with a custom endpoint
        var clientOptions = new OpenAI.OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com/v1")
        };
        var client = new ChatClient(model, new System.ClientModel.ApiKeyCredential(apiKey), clientOptions);

        var messages = new List<ChatMessage>();
        if (!string.IsNullOrEmpty(request.SystemPrompt))
            messages.Add(new SystemChatMessage(request.SystemPrompt));
        messages.Add(new UserChatMessage(request.Prompt));

        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = request.MaxTokens ?? 2048,
        };

        var completion = await client.CompleteChatAsync(messages, options, ct);
        var text = completion.Value.Content[0].Text;

        var usage = new LLMUsage(
            completion.Value.Usage.InputTokenCount,
            completion.Value.Usage.OutputTokenCount,
            completion.Value.Usage.TotalTokenCount);

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
            ?? throw new InvalidOperationException("Failed to deserialize DeepSeek structured output");
        return new LLMResponse<TOutput>(output, raw, textResponse.Usage);
    }
}
