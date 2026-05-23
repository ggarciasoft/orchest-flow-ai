using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;
namespace OrchestAI.Nodes.System;

public sealed class SystemStartNode : IWorkflowNode
{
    public string Type => "system.start";
    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var outputs = ctx.WorkflowInputs.ToDictionary(kv => kv.Key, kv => kv.Value);
        return Task.FromResult(NodeExecutionResult.Succeeded(outputs));
    }
}

public sealed class SystemStartNodeDescriptor : IWorkflowNodeDescriptor
{
    public string Type => "system.start";
    public string DisplayName => "Start";
    public string Description => "Entry point of the workflow. Surfaces workflow inputs.";
    public string Category => "system";
    public string Version => "1.0.0";
    public string? IconKey => "play";
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => Array.Empty<NodeOutputDefinition>();
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => Array.Empty<NodeConfigDefinition>();
}