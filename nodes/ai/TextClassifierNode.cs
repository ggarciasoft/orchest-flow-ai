using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.AI.Routing;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.AI;

/// <summary>
/// Classifies input text into one of the configured categories using an LLM.
/// Falls back to the closest match using Levenshtein distance if no exact match is found.
/// </summary>
public sealed class TextClassifierNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "ai.classify";

    /// <summary>
    /// Sends the input text to the configured LLM with a classification prompt.
    /// </summary>
    /// <param name="ctx">Execution context providing "text" input and "categories"/"model" config.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Succeeded result with "category", "confidence", and "rawResponse" outputs.</returns>
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var text = ctx.GetInput<string>("text") ?? throw new InvalidOperationException("Input 'text' is required");
        var categories = ctx.GetConfig<string>("categories") ?? throw new InvalidOperationException("Config 'categories' is required");
        var model = ctx.GetConfig<string>("model") ?? "default";
        var instructions = ctx.GetConfig<string>("instructions") ?? string.Empty;

        var router = ctx.Services.GetRequiredService<LLMProviderRouter>();
        var (provider, resolvedModel) = router.Route(model);

        var systemPrompt = $"You are a text classifier. Classify the given text into EXACTLY one of these categories: {categories}. Reply with ONLY the category name, nothing else.";
        if (!string.IsNullOrWhiteSpace(instructions))
            systemPrompt += $" Additional instructions: {instructions}";

        var response = await provider.GenerateTextAsync(
            new LLMRequest { Prompt = text, SystemPrompt = systemPrompt, Model = resolvedModel }, ct);

        var trimmed = response.Text.Trim().ToLowerInvariant();
        var categoryList = categories.Split(',').Select(c => c.Trim().ToLowerInvariant()).ToList();

        // Exact match = high confidence; closest Levenshtein = low confidence
        string matchedCategory;
        string confidence;
        if (categoryList.Contains(trimmed))
        {
            matchedCategory = trimmed;
            confidence = "high";
        }
        else
        {
            matchedCategory = categoryList.OrderBy(c => LevenshteinDistance(c, trimmed)).First();
            confidence = "low";
        }

        return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
        {
            ["category"] = matchedCategory,
            ["confidence"] = confidence,
            ["rawResponse"] = response.Text
        });
    }

    /// <summary>Computes Levenshtein edit distance between two strings for fuzzy category matching.</summary>
    private static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return int.MaxValue;
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        return d[a.Length, b.Length];
    }
}

/// <summary>Descriptor for <see cref="TextClassifierNode"/>.</summary>
public sealed class TextClassifierNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "ai.classify";
    /// <inheritdoc />
    public string DisplayName => "Text Classifier";
    /// <inheritdoc />
    public string Description => "Classifies text into one of the configured categories using an LLM.";
    /// <inheritdoc />
    public string Category => "ai";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "tag";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => new[]
    {
        new NodeInputDefinition("text", "Text", "The text to classify.", DataType.String, Required: true)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("category", "Category", "The matched category.", DataType.String),
        new NodeOutputDefinition("confidence", "Confidence", "'high' for exact match, 'low' for fuzzy.", DataType.String),
        new NodeOutputDefinition("rawResponse", "Raw Response", "Raw LLM response text.", DataType.String)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("categories", "Categories", "Comma-separated category names e.g. 'urgent,normal,low'.", DataType.String, Required: true),
        new NodeConfigDefinition("model", "Model", "LLM model to use.", DataType.String, Required: false, DefaultValue: "default", OptionsSource: "llm-models"),
        new NodeConfigDefinition("instructions", "Instructions", "Extra classification guidance.", DataType.String, Required: false)
    };
}
