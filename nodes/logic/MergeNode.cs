using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.Logic;

/// <summary>
/// Synchronization point that collects outputs from multiple upstream branches.
/// Forwards all node inputs as outputs, making merged data available to downstream nodes.
/// </summary>
public sealed class MergeNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "logic.merge";

    /// <summary>
    /// Passes all node inputs directly through as outputs.
    /// Acts as a convergence point after parallel or conditional branches.
    /// </summary>
    /// <param name="ctx">Execution context containing all inputs from upstream nodes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Succeeded result with all inputs forwarded as outputs.</returns>
    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        // Copy IReadOnlyDictionary to mutable Dictionary for NodeExecutionResult
        var outputs = ctx.NodeInputs.ToDictionary(kv => kv.Key, kv => kv.Value);
        return Task.FromResult(NodeExecutionResult.Succeeded(outputs));
    }
}

/// <summary>Descriptor for <see cref="MergeNode"/> — provides metadata for the designer palette.</summary>
public sealed class MergeNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "logic.merge";
    /// <inheritdoc />
    public string DisplayName => "Merge";
    /// <inheritdoc />
    public string Description => "Synchronizes multiple branches and forwards all inputs as outputs.";
    /// <inheritdoc />
    public string Category => "logic";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "merge";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => Array.Empty<NodeOutputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => Array.Empty<NodeConfigDefinition>();
}
