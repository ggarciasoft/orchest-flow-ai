using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;
using System.Text.Json;
namespace OrchestAI.Nodes.Human;

public sealed class HumanApprovalNode : IWorkflowNode
{
    public string Type => "human.approval";
    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var payload = ctx.NodeInputs.ToDictionary(kv => kv.Key, kv => kv.Value);
        payload["_approvalTitle"] = ctx.GetConfig<string>("title") ?? "Approval Required";
        return Task.FromResult(NodeExecutionResult.WaitingForApproval(payload));
    }
}

public sealed class HumanApprovalNodeDescriptor : IWorkflowNodeDescriptor
{
    public string Type => "human.approval";
    public string DisplayName => "Human Approval";
    public string Description => "Pauses the workflow and requests a human approve or reject decision.";
    public string Category => "human";
    public string Version => "1.0.0";
    public string? IconKey => "user-check";
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("decision", "Decision", "approved or rejected", DataType.String),
        new NodeOutputDefinition("comment", "Comment", "Approver comment", DataType.String),
        new NodeOutputDefinition("decidedBy", "Decided By", "User ID", DataType.String),
        new NodeOutputDefinition("decidedAt", "Decided At", "ISO timestamp", DataType.String)
    };
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("title", "Title", "Title shown to the approver.", DataType.String, Required: false, DefaultValue: "Approval Required"),
        new NodeConfigDefinition("assignees", "Assignees", "JSON array of user/role references.", DataType.Json, Required: false),
        new NodeConfigDefinition("slaMinutes", "SLA (minutes)", "Minutes before auto-expiry.", DataType.Number, Required: false)
    };
}