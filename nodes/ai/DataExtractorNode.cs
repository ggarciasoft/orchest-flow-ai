using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.AI.Routing;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

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
        var textInputKey = ctx.GetConfig<string>("textInput");
        // Guard against empty string config value — treat same as missing
        if (string.IsNullOrWhiteSpace(textInputKey)) textInputKey = "text";

        var rawText = ctx.GetInput<string>(textInputKey)
            ?? (textInputKey == "text" ? ctx.GetInput<string>("item") ?? ctx.GetInput<string>("body") : null);

        // Fallback: when NodeInputs is empty (ResolveInputs didn't propagate the ForEach item),
        // look directly in NodeOutputs for the injected 'item' key from the loop engine.
        if (rawText == null)
        {
            foreach (var nodeOut in ctx.NodeOutputs.Values)
            {
                if (nodeOut.TryGetValue("item", out var itemVal) && itemVal != null)
                { rawText = itemVal?.ToString(); break; }
                if (!string.IsNullOrEmpty(textInputKey) && textInputKey != "item"
                    && nodeOut.TryGetValue(textInputKey, out var keyVal) && keyVal != null)
                { rawText = keyVal?.ToString(); break; }
            }
        }

        if (rawText == null)
            throw new InvalidOperationException($"Input '{textInputKey}' is required");

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

        // Strip HTML tags so the LLM receives clean plain text
        text = Regex.Replace(text, "<[^>]+>", " ");
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\s{2,}", " ").Trim();
        var fields = ctx.GetConfig<string>("fields") ?? throw new InvalidOperationException("Config 'fields' is required");
        var formatInstructions = ctx.GetConfig<string>("formatInstructions");
        var model = ctx.GetConfig<string>("model") ?? "default";

        var router = ctx.Services.GetRequiredService<LLMProviderRouter>();
        var (provider, resolvedModel) = router.Route(model);

        var formatClause = string.IsNullOrWhiteSpace(formatInstructions)
            ? string.Empty
            : $"\n\nFormat rules (apply strictly):\n{formatInstructions}";
        var prompt = $"Extract the following fields from the text and return as a JSON object with exactly these keys: {fields}.{formatClause}\n\nText:\n{{text}}\n\nReturn ONLY valid JSON, no markdown, no explanation.";
        prompt = prompt.Replace("{text}", text);
        var response = await provider.GenerateTextAsync(
            new LLMRequest { Prompt = prompt, Model = resolvedModel, MaxTokens = 1024, TenantId = ctx.TenantId }, ct);

        // Strip markdown code fences if the LLM returned them despite instructions
        var extractedJson = response.Text.Trim();
        if (extractedJson.StartsWith("```"))
        {
            var firstNewline = extractedJson.IndexOf('\n');
            var lastFence = extractedJson.LastIndexOf("```");
            if (firstNewline >= 0 && lastFence > firstNewline)
                extractedJson = extractedJson[(firstNewline + 1)..lastFence].Trim();
        }

        var outputs = new Dictionary<string, object?> { ["extractedJson"] = extractedJson };

        // Parse individual fields and surface as separate outputs
        try
        {
            var doc = JsonDocument.Parse(extractedJson);
            foreach (var field in fields.Split(',').Select(f => f.Trim()))
            {
                if (doc.RootElement.TryGetProperty(field, out var val))
                    outputs[field] = val.ValueKind == JsonValueKind.Null ? null : val.GetString();
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
        new NodeConfigDefinition("formatInstructions", "Format Instructions", "Optional per-field format rules appended to the LLM prompt. E.g. 'Amount: numeric only, no currency symbols or commas. Date: ISO 8601 (YYYY-MM-DD). Category: one of Food, Transport, Utilities, Other.'.", DataType.String, Required: false),
        new NodeConfigDefinition("textInput", "Text Input", "Which upstream output to use as the text source.", DataType.Enum, Required: false, DefaultValue: "text", AllowedValues: new[] { "text", "item", "body" }),
        new NodeConfigDefinition("model", "Model", "LLM model to use.", DataType.String, Required: false, DefaultValue: "default", OptionsSource: "llm-models")
    };
}
