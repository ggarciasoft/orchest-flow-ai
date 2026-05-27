namespace OrchestFlowAI.AI.Abstractions;
public sealed record LLMRequest { public string Prompt { get; init; } = default!; public string? SystemPrompt { get; init; } public string Model { get; init; } = "default"; public double? Temperature { get; init; } public int? MaxTokens { get; init; } public Guid? TenantId { get; init; } }
public sealed record LLMUsage(int PromptTokens, int CompletionTokens, int TotalTokens, decimal? EstimatedCostUsd = null);
public sealed record LLMResponse(string Text, LLMUsage Usage);
public sealed record LLMResponse<T>(T Output, string RawText, LLMUsage Usage);

public interface ILLMProvider
{
    string Id { get; }
    IReadOnlyCollection<string> Models { get; }
    Task<LLMResponse> GenerateTextAsync(LLMRequest request, CancellationToken ct = default);
    Task<LLMResponse<TOutput>> GenerateStructuredAsync<TOutput>(LLMRequest request, string jsonSchema, CancellationToken ct = default);
}