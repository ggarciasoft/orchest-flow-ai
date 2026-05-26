using System.Text.Json;
using OpenAI.Chat;
using OrchestFlowAI.AI.Abstractions;
using Microsoft.Extensions.Logging;
namespace OrchestFlowAI.AI.Providers;

public sealed class OpenAILLMProvider : ILLMProvider
{
    private readonly OpenAIApiKeyHolder _keyHolder;
    private readonly ILogger<OpenAILLMProvider> _logger;
    public string Id => "openai";
    public IReadOnlyCollection<string> Models => new[] { "gpt-4o", "gpt-4o-mini", "gpt-3.5-turbo" };

    public OpenAILLMProvider(OpenAIApiKeyHolder keyHolder, ILogger<OpenAILLMProvider> logger)
    {
        _keyHolder = keyHolder;
        _logger = logger;
    }

    public async Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken ct = default)
    {
        var model = request.Model == "default" ? "gpt-4o-mini" : request.Model;
        var client = new ChatClient(model, _keyHolder.ApiKey);
        var messages = new List<ChatMessage>();
        if (request.SystemPrompt != null) messages.Add(ChatMessage.CreateSystemMessage(request.SystemPrompt));
        messages.Add(ChatMessage.CreateUserMessage(request.Prompt));
        var options = new ChatCompletionOptions();
        if (request.MaxTokens.HasValue) options.MaxOutputTokenCount = request.MaxTokens.Value;
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

        // Strip markdown code fences if present
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
