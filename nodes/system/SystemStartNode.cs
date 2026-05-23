using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;
namespace OrchestAI.Nodes.System;

/// <summary>
/// Entry point node for every workflow execution.
/// Passes all workflow-level input values through as node outputs,
/// making them available to downstream nodes via edge mappings.
/// </summary>
public sealed class SystemStartNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "system.start";

    /// <summary>
    /// Surfaces all workflow inputs as outputs so downstream nodes can consume them.
    /// </summary>
    /// <param name="ctx">Execution context containing the workflow's input payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Succeeded result with all workflow inputs forwarded as outputs.</returns>
    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        // Forward all workflow-level inputs directly as this node's outputs
        var outputs = ctx.WorkflowInputs.ToDictionary(kv => kv.Key, kv => kv.Value);
        return Task.FromResult(NodeExecutionResult.Succeeded(outputs));
    }
}

/// <summary>Descriptor for <see cref="SystemStartNode"/> — provides metadata for the designer palette.</summary>
public sealed class SystemStartNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "system.start";
    /// <inheritdoc />
    public string DisplayName => "Start";
    /// <inheritdoc />
    public string Description => "Entry point of the workflow. Surfaces workflow inputs.";
    /// <inheritdoc />
    public string Category => "system";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "play";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => Array.Empty<NodeOutputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => Array.Empty<NodeConfigDefinition>();
}
