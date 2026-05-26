using System.Text.Json;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.Logic;

/// <summary>
/// Fan-out node that expands a JSON array into indexed outputs (item_0..N), plus count, firstItem, and items.
/// Because the engine does not support sub-graph looping, all items are expanded inline.
/// Downstream nodes that need per-item processing should be wired to individual item_N outputs
/// or consume the full <c>items</c> JSON array.
/// </summary>
public sealed class ForEachNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "logic.foreach";

    /// <inheritdoc />
    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        // Resolve the input array from node inputs
        var rawInput = ctx.NodeInputs.TryGetValue("inputArray", out var v) ? v : null;

        List<JsonElement> items;
        try
        {
            items = ParseArray(rawInput);
        }
        catch (Exception ex)
        {
            return Task.FromResult(NodeExecutionResult.Failed($"Failed to parse inputArray: {ex.Message}"));
        }

        var maxItems = (int)(ctx.GetConfig<double?>("maxItems") ?? 50.0);
        if (maxItems < 1) maxItems = 1;
        if (maxItems > 200) maxItems = 200;

        var capped = items.Take(maxItems).ToList();

        var outputs = new Dictionary<string, object?>();
        outputs["count"] = capped.Count;
        outputs["items"] = JsonSerializer.Serialize(capped);
        outputs["firstItem"] = capped.Count > 0 ? capped[0].GetRawText() : null;

        for (var i = 0; i < capped.Count; i++)
        {
            outputs[$"item_{i}"] = capped[i].GetRawText();
        }

        return Task.FromResult(NodeExecutionResult.Succeeded(outputs));
    }

    private static List<JsonElement> ParseArray(object? raw)
    {
        if (raw is null)
            return new List<JsonElement>();

        // Already a JsonElement
        if (raw is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Array)
                return je.EnumerateArray().ToList();
            // Maybe it's a string containing JSON
            if (je.ValueKind == JsonValueKind.String)
                return ParseJsonString(je.GetString() ?? "[]");
            return new List<JsonElement>();
        }

        // Plain string
        if (raw is string s)
            return ParseJsonString(s);

        // Try to serialize and re-parse
        var json = JsonSerializer.Serialize(raw);
        return ParseJsonString(json);
    }

    private static List<JsonElement> ParseJsonString(string json)
    {
        json = json.Trim();
        if (string.IsNullOrEmpty(json)) return new List<JsonElement>();

        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
            return doc.RootElement.EnumerateArray().ToList();

        // Single object — wrap it
        return new List<JsonElement> { doc.RootElement };
    }
}

/// <summary>Descriptor for <see cref="ForEachNode"/>.</summary>
public sealed class ForEachNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "logic.foreach";
    /// <inheritdoc />
    public string DisplayName => "For Each";
    /// <inheritdoc />
    public string Description => "Expands a JSON array into indexed outputs (item_0..N). Useful for fan-out processing of lists from previous nodes.";
    /// <inheritdoc />
    public string Category => "logic";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "loop";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => new[]
    {
        new NodeInputDefinition("inputArray", "Input Array", "JSON array string (or serialized array) to iterate over.", DataType.String, Required: true)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("items", "Items", "Full JSON array string of all processed items.", DataType.String),
        new NodeOutputDefinition("count", "Count", "Number of items in the array.", DataType.Number),
        new NodeOutputDefinition("firstItem", "First Item", "JSON string of the first item (convenience output).", DataType.String),
        new NodeOutputDefinition("item_0", "Item 0", "JSON string of the first item.", DataType.String),
        new NodeOutputDefinition("item_1", "Item 1", "JSON string of the second item.", DataType.String),
        new NodeOutputDefinition("item_2", "Item 2", "JSON string of the third item.", DataType.String)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("itemVariable", "Item Variable", "Name prefix for per-item outputs (default: item).", DataType.String, Required: false, DefaultValue: "item"),
        new NodeConfigDefinition("maxItems", "Max Items", "Maximum number of items to expand (default: 50, max: 200).", DataType.Number, Required: false, DefaultValue: 50)
    };
}
