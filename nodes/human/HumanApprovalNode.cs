using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;
using System.Text.Json;
namespace OrchestAI.Nodes.Human;

/// <summary>
/// Pauses workflow execution and creates a human approval request.
/// The workflow resumes only after an approver submits an approve or reject decision
/// via the Approvals API. The approval payload is forwarded to the approval inbox UI.
/// </summary>
public sealed class HumanApprovalNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "human.approval";

    /// <summary>
    /// Builds the approval payload from node inputs and suspends execution waiting for human review.
    /// </summary>
    /// <param name="ctx">Execution context providing config ("title") and node inputs as payload data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>WaitingForApproval result containing the payload that will be shown to the approver.</returns>
    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        // Include all node inputs in the approval payload so the approver can see the context
        var payload = ctx.NodeInputs.ToDictionary(kv => kv.Key, kv => kv.Value);
        // _approvalTitle is a system key read by the UI to display a meaningful heading
        payload["_approvalTitle"] = ctx.GetConfig<string>("title") ?? "Approval Required";
        return Task.FromResult(NodeExecutionResult.WaitingForApproval(payload));
    }
}

/// <summary>Descriptor for <see cref="HumanApprovalNode"/> — provides metadata for the designer palette.</summary>
public sealed class HumanApprovalNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "human.approval";
    /// <inheritdoc />
    public string DisplayName => "Human Approval";
    /// <inheritdoc />
    public string Description => "Pauses the workflow and requests a human approve or reject decision.";
    /// <inheritdoc />
    public string Category => "human";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "user-check";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("decision", "Decision", "approved or rejected", DataType.String),
        new NodeOutputDefinition("comment", "Comment", "Approver comment", DataType.String),
        new NodeOutputDefinition("decidedBy", "Decided By", "User ID", DataType.String),
        new NodeOutputDefinition("decidedAt", "Decided At", "ISO timestamp", DataType.String)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("title", "Title", "Title shown to the approver.", DataType.String, Required: false, DefaultValue: "Approval Required"),
        new NodeConfigDefinition("assignees", "Assignees", "JSON array of user/role references.", DataType.Json, Required: false),
        new NodeConfigDefinition("slaMinutes", "SLA (minutes)", "Minutes before auto-expiry.", DataType.Number, Required: false)
    };
}
