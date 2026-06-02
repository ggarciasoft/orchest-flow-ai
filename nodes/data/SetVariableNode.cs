using System.Text.Json;
using System.Text.RegularExpressions;
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

    // Matches unquoted JSON keys: word chars at the start of a property name
    private static readonly Regex _unquotedKey = new(@"(?<=[{,]\s*)([A-Za-z_][\w]*)(?=\s*:)", RegexOptions.Compiled);

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var variableConfig = ctx.GetConfig<string>("variables") ?? throw new InvalidOperationException("variables config is required.");

        Dictionary<string, string>? variables = null;
        try
        {
            variables = JsonSerializer.Deserialize<Dictionary<string, string>>(variableConfig);
        }
        catch (JsonException)
        {
            // Auto-fix unquoted keys (e.g. {query: "..."} → {"query": "..."})
            var fixed_ = _unquotedKey.Replace(variableConfig, m => $"\"{m.Value}\"");
            variables = JsonSerializer.Deserialize<Dictionary<string, string>>(fixed_);
        }

        if (variables == null) return NodeExecutionResult.Failed("Invalid JSON in variables config.");

        var outputs = new Dictionary<string, object?>();
        foreach (var (key, template) in variables)
            outputs[key] = ReplacePlaceholders(template, ctx.NodeInputs);

        return await Task.FromResult(NodeExecutionResult.Succeeded(outputs));
    }

    private static string ReplacePlaceholders(string input, IReadOnlyDictionary<string, object?> nodeInputs)
    {
        foreach (var (key, value) in nodeInputs)
            input = input.Replace($"{{{{{key}}}}}", value?.ToString() ?? string.Empty);
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
        new NodeConfigDefinition("variables", "Variables", "JSON key-value pairs with placeholders. Keys may be unquoted. Example: {\"query\": \"value {{input}}\"}.", DataType.String, true, IsMultiline: true)
    };
}