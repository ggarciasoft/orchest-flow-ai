using System.Text.Json;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.Data;

/// <summary>
/// Sets variables by resolving placeholders and creating dynamic outputs.
/// </summary>
public sealed class SetVariableNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "data.set";

    /// <summary>
    /// Resolves placeholders in variables and outputs dynamic key-value pairs.
    /// </summary>
    /// <param name="ctx">Node execution context, including configuration and inputs.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A succeeded result with all resolved key-value pairs as outputs.</returns>
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        // Retrieve and parse the "variables" config
        var variableConfig = ctx.GetConfig<string>("variables") ?? throw new InvalidOperationException("variables config is required.");
        var variables = JsonSerializer.Deserialize<Dictionary<string, string>>(variableConfig)
                        ?? throw new InvalidOperationException("Invalid JSON in variables config.");

        // Resolve placeholders
        var outputs = new Dictionary<string, object?>();
        foreach (var (key, template) in variables)
        {
            outputs[key] = ReplacePlaceholders(template, ctx.NodeInputs);
        }

        return await Task.FromResult(NodeExecutionResult.Succeeded(outputs));
    }

    private static string ReplacePlaceholders(string input, IReadOnlyDictionary<string, object?> nodeInputs)
    {
        foreach (var (key, value) in nodeInputs)
        {
            input = input.Replace($"{{{{{key}}}}}", value?.ToString() ?? string.Empty);
        }
        return input;
    }
}

/// <summary>
/// Descriptor for the SetVariableNode — defines metadata for the designer.
/// </summary>
public sealed class SetVariableNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "data.set";
    /// <inheritdoc />
    public string DisplayName => "Set Variable";
    /// <inheritdoc />
    public string Description => "Sets variables using key-value pairs with placeholder resolution.";
    /// <inheritdoc />
    public string Category => "data";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "variable";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => Array.Empty<NodeOutputDefinition>();  // Dynamic outputs
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("variables", "Variables", "JSON key-value pairs with placeholders.", DataType.String, true)
    };
}