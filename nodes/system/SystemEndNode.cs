using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;
namespace OrchestAI.Nodes.System;

public sealed class SystemEndNode : IWorkflowNode
{
    public string Type => "system.end";
    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
        => Task.FromResult(NodeExecutionResult.Succeeded(ctx.NodeInputs.ToDictionary(kv => kv.Key, kv => kv.Value)));
}

public sealed class SystemEndNodeDescriptor : IWorkflowNodeDescriptor
{
    public string Type => "system.end";
    public string DisplayName => "End";
    public string Description => "Terminal node. Marks workflow completion.";
    public string Category => "system";
    public string Version => "1.0.0";
    public string? IconKey => "stop";
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => Array.Empty<NodeOutputDefinition>();
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => Array.Empty<NodeConfigDefinition>();
}