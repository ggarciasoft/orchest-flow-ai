using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;
namespace OrchestFlowAI.Nodes.System;

/// <summary>
/// Terminal node that marks workflow completion.
/// Passes its inputs through as outputs to preserve the final data state in the execution record.
/// A valid workflow must contain at least one system.end node.
/// </summary>
public sealed class SystemEndNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "system.end";

    /// <summary>
    /// Marks the workflow as complete by returning a successful result with all inputs forwarded.
    /// </summary>
    /// <param name="ctx">Execution context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Succeeded result with inputs passed through as final outputs.</returns>
    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
        => Task.FromResult(NodeExecutionResult.Succeeded(ctx.NodeInputs.ToDictionary(kv => kv.Key, kv => kv.Value)));
}

/// <summary>Descriptor for <see cref="SystemEndNode"/> — provides metadata for the designer palette.</summary>
public sealed class SystemEndNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "system.end";
    /// <inheritdoc />
    public string DisplayName => "End";
    /// <inheritdoc />
    public string Description => "Terminal node. Marks workflow completion.";
    /// <inheritdoc />
    public string Category => "system";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "stop";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => Array.Empty<NodeOutputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => Array.Empty<NodeConfigDefinition>();
}
