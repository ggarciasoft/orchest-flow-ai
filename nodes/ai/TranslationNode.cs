using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.AI.Routing;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.AI;

/// <summary>
/// Translates input text into a target language using an LLM.
/// </summary>
public sealed class TranslationNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "ai.translate";

    /// <summary>
    /// Sends the text to the LLM with a translation prompt targeting the configured language.
    /// </summary>
    /// <param name="ctx">Execution context providing "text" input and "targetLanguage"/"model" config.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Succeeded result with "translatedText" and "targetLanguage" outputs.</returns>
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var text = ctx.GetInput<string>("text") ?? throw new InvalidOperationException("Input 'text' is required");
        var targetLanguage = ctx.GetConfig<string>("targetLanguage") ?? throw new InvalidOperationException("Config 'targetLanguage' is required");
        var model = ctx.GetConfig<string>("model") ?? "default";

        var router = ctx.Services.GetRequiredService<LLMProviderRouter>();
        var (provider, resolvedModel) = router.Route(model);

        var systemPrompt = $"You are a professional translator. Translate the text accurately into {targetLanguage}. Respond with ONLY the translated text, no explanation.";
        var response = await provider.GenerateTextAsync(
            new LLMRequest { Prompt = text, SystemPrompt = systemPrompt, Model = resolvedModel, MaxTokens = 1024 }, ct);

        return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
        {
            ["translatedText"] = response.Text.Trim(),
            ["targetLanguage"] = targetLanguage
        });
    }
}

/// <summary>Descriptor for <see cref="TranslationNode"/>.</summary>
public sealed class TranslationNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "ai.translate";
    /// <inheritdoc />
    public string DisplayName => "Translation";
    /// <inheritdoc />
    public string Description => "Translates text into a specified language using an LLM.";
    /// <inheritdoc />
    public string Category => "ai";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "languages";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => new[]
    {
        new NodeInputDefinition("text", "Text", "The text to translate.", DataType.String, Required: true)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("translatedText", "Translated Text", "The translated text.", DataType.String),
        new NodeOutputDefinition("targetLanguage", "Target Language", "The language the text was translated into.", DataType.String)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("targetLanguage", "Target Language", "Language to translate into e.g. 'Spanish', 'French'.", DataType.String, Required: true),
        new NodeConfigDefinition("model", "Model", "LLM model to use.", DataType.String, Required: false, DefaultValue: "default")
    };
}
