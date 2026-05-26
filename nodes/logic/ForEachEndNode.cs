using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.Logic;

/// <summary>
/// Marks the end of a ForEach loop body.
/// Passes all inputs through as outputs unchanged.
/// Wire this after the last node in the loop body to signal the engine where the loop ends.
/// </summary>
public sealed class ForEachEndNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "logic.foreach.end";

    /// <inheritdoc />
    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
        => Task.FromResult(NodeExecutionResult.Succeeded(
            ctx.NodeInputs.ToDictionary(kv => kv.Key, kv => kv.Value)));
}

/// <summary>Descriptor for <see cref="ForEachEndNode"/>.</summary>
public sealed class ForEachEndNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "logic.foreach.end";
    /// <inheritdoc />
    public string DisplayName => "ForEach End";
    /// <inheritdoc />
    public string Description => "Marks the end of a ForEach loop body. Wire after the last node in the loop body. The engine collects outputs from this node as the per-item result.";
    /// <inheritdoc />
    public string Category => "logic";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "chevrons-right";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => new[]
    {
        new NodeInputDefinition("*", "Any", "All inputs are passed through as outputs.", DataType.Json, Required: false)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("*", "Any", "All inputs passed through as outputs unchanged.", DataType.Json)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => Array.Empty<NodeConfigDefinition>();
}
