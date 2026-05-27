using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.AI.Routing;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;
using System.Text.Json;

namespace OrchestFlowAI.Nodes.AI;

/// <summary>
/// Extracts structured fields from unstructured text using an LLM.
/// Returns results as a JSON object and individual field outputs.
/// </summary>
public sealed class DataExtractorNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "ai.extract";

    /// <summary>
    /// Prompts the LLM to extract the configured fields from the input text as a JSON object.
    /// </summary>
    /// <param name="ctx">Execution context providing "text" input and "fields"/"model" config.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Succeeded result with "extractedJson" and individual field outputs.</returns>
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        // Accept 'text' directly, or fall back to 'item' (ForEach loop output) or 'body' (email body)
        // If 'item' is a JSON object with a 'body' field (e.g. Gmail email), extract just the body
        var textInputKey = ctx.GetConfig<string>("textInput") ?? "text";
        var rawText = ctx.GetInput<string>(textInputKey)
            ?? (textInputKey == "text" ? ctx.GetInput<string>("item") ?? ctx.GetInput<string>("body") : null)
            ?? throw new InvalidOperationException($"Input '{textInputKey}' is required");

        // If the input looks like a JSON email object, prefer the body field for extraction
        string text;
        try
        {
            var doc = JsonDocument.Parse(rawText);
            text = doc.RootElement.TryGetProperty("body", out var bodyEl) && bodyEl.ValueKind == JsonValueKind.String
                ? bodyEl.GetString() ?? rawText
                : rawText;
        }
        catch (JsonException) { text = rawText; }
        var fields = ctx.GetConfig<string>("fields") ?? throw new InvalidOperationException("Config 'fields' is required");
        var model = ctx.GetConfig<string>("model") ?? "default";

        var router = ctx.Services.GetRequiredService<LLMProviderRouter>();
        var (provider, resolvedModel) = router.Route(model);

        var prompt = $"Extract the following fields from the text and return as a JSON object with exactly these keys: {fields}.\n\nText:\n{text}\n\nReturn ONLY valid JSON, no explanation.";
        var response = await provider.GenerateTextAsync(
            new LLMRequest { Prompt = prompt, Model = resolvedModel, MaxTokens = 1024 }, ct);

        var extractedJson = response.Text.Trim();
        var outputs = new Dictionary<string, object?> { ["extractedJson"] = extractedJson };

        // Attempt to parse JSON and surface individual fields as separate outputs
        try
        {
            var doc = JsonDocument.Parse(extractedJson);
            foreach (var field in fields.Split(',').Select(f => f.Trim()))
            {
                if (doc.RootElement.TryGetProperty(field, out var val))
                    outputs[field] = val.GetString();
            }
        }
        catch (JsonException) { /* Leave extractedJson as raw string if not parseable */ }

        return NodeExecutionResult.Succeeded(outputs);
    }
}

/// <summary>Descriptor for <see cref="DataExtractorNode"/>.</summary>
public sealed class DataExtractorNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "ai.extract";
    /// <inheritdoc />
    public string DisplayName => "Data Extractor";
    /// <inheritdoc />
    public string Description => "Extracts structured fields from unstructured text using an LLM.";
    /// <inheritdoc />
    public string Category => "ai";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "database";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => new[]
    {
        new NodeInputDefinition("text", "Text", "The text to extract data from.", DataType.String, Required: true)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("extractedJson", "Extracted JSON", "All extracted fields as a JSON object.", DataType.Json)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("fields", "Fields", "Comma-separated field names to extract e.g. 'name,date,amount'.", DataType.String, Required: true),
        new NodeConfigDefinition("textInput", "Text Input", "Which upstream output to use as the text source.", DataType.Enum, Required: false, DefaultValue: "text", AllowedValues: new[] { "text", "item", "body" }),
        new NodeConfigDefinition("model", "Model", "LLM model to use.", DataType.String, Required: false, DefaultValue: "default", OptionsSource: "llm-models")
    };
}
