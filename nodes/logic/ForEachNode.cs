using System.Text.Json;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.Logic;

/// <summary>
/// Iterates a JSON array. Supports two modes:
/// <list type="bullet">
/// <item><b>Fan-out mode (default)</b>: expands the array into indexed outputs (item_0..N) for static wiring.</item>
/// <item><b>Loop mode</b> (<c>loopMode=true</c>): emits <c>_foreach_items</c> so the engine executes downstream
/// nodes once per item and collects results. Wire a <c>logic.foreach.end</c> node at the end of the loop body.</item>
/// </list>
/// </summary>
public sealed class ForEachNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "logic.foreach";

    /// <inheritdoc />
    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        // Resolve the input array from node inputs.
        // Accept 'inputArray' (canonical), or common aliases when the upstream node uses a different key.
        object? rawInput = null;
        foreach (var key in new[] { "inputArray", "emails", "items", "data", "array", "results" })
        {
            if (ctx.NodeInputs.TryGetValue(key, out var v) && v != null) { rawInput = v; break; }
        }

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

        // Loop mode: signal the engine to iterate the loop body per item
        var loopMode = ctx.GetConfig<bool?>("loopMode") ?? false;
        if (loopMode)
        {
            outputs["_foreach_items"] = JsonSerializer.Serialize(capped);
            outputs["_foreach_loop_mode"] = (object?)true;
            // Still emit fan-out for convenience / backward compat
        }

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
        new NodeOutputDefinition("item_2", "Item 2", "JSON string of the third item.", DataType.String),
        new NodeOutputDefinition("_foreach_items", "ForEach Items (loop mode)", "JSON array emitted when loopMode=true; consumed by the engine to run per-item subgraph iteration.", DataType.String),
        new NodeOutputDefinition("results", "Results (loop mode)", "Collected per-item outputs after the engine finishes looping (replaces _foreach_items in nodeOutputs).", DataType.Json)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("itemVariable", "Item Variable", "Name prefix for per-item outputs (default: item).", DataType.String, Required: false, DefaultValue: "item"),
        new NodeConfigDefinition("maxItems", "Max Items", "Maximum number of items to expand (default: 50, max: 200).", DataType.Number, Required: false, DefaultValue: 50),
        new NodeConfigDefinition("loopMode", "Loop Mode", "When true, the engine executes downstream nodes once per item instead of fan-out. Requires a logic.foreach.end node at the end of the loop body.", DataType.Boolean, Required: false, DefaultValue: false)
    };
}
