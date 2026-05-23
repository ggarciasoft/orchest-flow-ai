using System.Text.Json;
using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;

namespace OrchestAI.Nodes.Data;

/// <summary>
/// Transforms JSON using a mapping configuration to create a new structure.
/// </summary>
public sealed class JsonTransformNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "data.json-transform";

    /// <summary>
    /// Transforms input JSON based on the provided mapping configuration.
    /// </summary>
    /// <param name="ctx">Node execution context, including configuration and inputs.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A succeeded result with transformed JSON and dynamic fields.</returns>
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        // Get required input and config
        var inputJson = ctx.GetInput<string>("json") ?? throw new InvalidOperationException("Input JSON is required.");
        var mappingConfig = ctx.GetConfig<string>("mapping") ?? throw new InvalidOperationException("Mapping config is required.");

        var parsedJson = JsonSerializer.Deserialize<Dictionary<string, object?>>(inputJson)
                         ?? throw new InvalidOperationException("Invalid JSON input.");
        var mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(mappingConfig)
                       ?? throw new InvalidOperationException("Invalid JSON in mapping config.");

        // Apply transformations
        var transformed = new Dictionary<string, object?>();
        foreach (var (key, path) in mappings)
        {
            transformed[key] = ResolvePath(parsedJson, path);
        }

        // Add the full transformed object
        transformed["transformedJson"] = JsonSerializer.Serialize(transformed);
        return await Task.FromResult(NodeExecutionResult.Succeeded(transformed));
    }

    private static object? ResolvePath(Dictionary<string, object?> json, string path)
    {
        var parts = path.Split('.');
        object? current = json;

        foreach (var part in parts)
        {
            if (current is not Dictionary<string, object?> dict || !dict.TryGetValue(part, out current))
            {
                return null; // Path not found
            }
        }

        return current;
    }
}

/// <summary>
/// Descriptor for the JsonTransformNode — defines metadata for the designer.
/// </summary>
public sealed class JsonTransformNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "data.json-transform";
    /// <inheritdoc />
    public string DisplayName => "JSON Transform";
    /// <inheritdoc />
    public string Description => "Transforms JSON into a new structure based on a mapping configuration.";
    /// <inheritdoc />
    public string Category => "data";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "braces";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => new[]
    {
        new NodeInputDefinition("json", "JSON Input", "JSON string to transform.", DataType.String, true)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("transformedJson", "Transformed JSON", "Resulting mapped object as JSON.", DataType.String)
    };  // Outputs are dynamic
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("mapping", "Mapping", "JSON object mapping output fields to input paths.", DataType.String, true)
    };
}