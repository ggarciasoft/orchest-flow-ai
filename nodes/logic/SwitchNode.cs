using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;

namespace OrchestAI.Nodes.Logic;

/// <summary>
/// Routes workflow execution based on a string input value matched against configured cases.
/// Outputs the matched case name and a boolean indicating whether a match was found.
/// </summary>
public sealed class SwitchNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "logic.switch";

    /// <summary>
    /// Compares the "value" input against the comma-separated "cases" config.
    /// Outputs the matched case or "default" if no match is found.
    /// </summary>
    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var value = ctx.GetInput<string>("value") ?? string.Empty;
        var cases = (ctx.GetConfig<string>("cases") ?? string.Empty)
            .Split(',')
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrEmpty(c));

        var matchedCase = cases.FirstOrDefault(c => c.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? "default";
        return Task.FromResult(NodeExecutionResult.Succeeded(new Dictionary<string, object?>
        {
            ["matched"] = matchedCase != "default",
            ["matchedCase"] = matchedCase
        }));
    }
}

/// <summary>Descriptor for <see cref="SwitchNode"/>.</summary>
public sealed class SwitchNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "logic.switch";
    /// <inheritdoc />
    public string DisplayName => "Switch";
    /// <inheritdoc />
    public string Description => "Routes execution to the matching branch based on an input value.";
    /// <inheritdoc />
    public string Category => "logic";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "git-branch";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => new[]
    {
        new NodeInputDefinition("value", "Value", "The value to match against cases.", DataType.String, Required: true)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("matchedCase", "Matched Case", "The matched case or 'default' if none matched.", DataType.String),
        new NodeOutputDefinition("matched", "Matched", "True if a case was matched.", DataType.Boolean)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("cases", "Cases", "Comma-separated list of expected values.", DataType.String, Required: true)
    };
}
