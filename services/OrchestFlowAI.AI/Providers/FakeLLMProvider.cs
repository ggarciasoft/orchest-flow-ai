using OrchestFlowAI.AI.Abstractions;
using System.Text.Json;
namespace OrchestFlowAI.AI.Providers;

public sealed class FakeLLMProvider : ILLMProvider
{
    private readonly Dictionary<string, string> _responses;
    public string Id => "fake";
    public IReadOnlyCollection<string> Models => new[] { "fake-model" };

    public FakeLLMProvider(Dictionary<string, string>? responses = null)
        => _responses = responses ?? new();

    public Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken ct = default)
    {
        var key = _responses.Keys.FirstOrDefault(k => request.Prompt.Contains(k));
        var text = key != null ? _responses[key] : "Fake response";
        return Task.FromResult(new LLMResponse(text, new LLMUsage(10, 20, 30, 0.001m)));
    }

    public Task<LLMResponse<TOutput>> GenerateStructuredAsync<TOutput>(LLMRequest request, string jsonSchema, CancellationToken ct = default)
    {
        var key = _responses.Keys.FirstOrDefault(k => request.Prompt.Contains(k));
        var raw = key != null ? _responses[key] : "{}";
        var output = JsonSerializer.Deserialize<TOutput>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        return Task.FromResult(new LLMResponse<TOutput>(output, raw, new LLMUsage(10, 20, 30)));
    }
}