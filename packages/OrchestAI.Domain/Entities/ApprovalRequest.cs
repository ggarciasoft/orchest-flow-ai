using OrchestAI.Domain.Enums;

namespace OrchestAI.Domain.Entities;

/// <summary>
/// Represents a request for approval within a workflow execution.
/// Contains details of the approval process, its state, and lifecycle events.
/// </summary>
public sealed class ApprovalRequest
{
    /// <summary>
    /// Gets the unique identifier of this approval request.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the tenant associated with this approval request.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the workflow execution associated with this approval request.
    /// </summary>
    public Guid WorkflowExecutionId { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the node execution associated with this approval request.
    /// </summary>
    public Guid NodeExecutionId { get; private set; }

    /// <summary>
    /// Gets the current status of the approval request.
    /// </summary>
    public ApprovalStatus Status { get; private set; }

    /// <summary>
    /// Gets the payload data for the approval request in serialized JSON format.
    /// </summary>
    public string PayloadJson { get; private set; } = "{}";

    /// <summary>
    /// Gets the serialized JSON list of assigned approvers, if applicable.
    /// </summary>
    public string? AssigneesJson { get; private set; }

    /// <summary>
    /// Gets the date and time when the approval request was created.
    /// </summary>
    public DateTime RequestedAt { get; private set; }

    /// <summary>
    /// Gets the date and time when the approval request was responded to, if applicable.
    /// </summary>
    public DateTime? RespondedAt { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the user or entity that made the approval request, if applicable.
    /// </summary>
    public Guid? RequestedBy { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the user or entity that responded to the approval request, if applicable.
    /// </summary>
    public Guid? RespondedBy { get; private set; }

    /// <summary>
    /// Gets the decision made on the approval request (e.g., approved, rejected), if applicable.
    /// </summary>
    public string? Decision { get; private set; }

    /// <summary>
    /// Gets any comments made when responding to the approval request, if applicable.
    /// </summary>
    public string? Comment { get; private set; }

    /// <summary>
    /// Gets the service-level agreement (SLA) time in minutes for responding to the approval request, if applicable.
    /// </summary>
    public int? SlaMinutes { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalRequest"/> class.
    /// </summary>
    private ApprovalRequest() { }

    /// <summary>
    /// Factory method to create a new approval request.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant associated with this approval request.</param>
    /// <param name="workflowExecutionId">The unique identifier of the workflow execution associated with this approval request.</param>
    /// <param name="nodeExecutionId">The unique identifier of the node execution associated with this approval request.</param>
    /// <param name="payloadJson">The payload data for the approval request in serialized JSON format.</param>
    /// <param name="requestedBy">The unique identifier of the user or entity that made the approval request.</param>
    /// <param name="slaMinutes">The SLA time in minutes for responding to the approval request (optional).</param>
    /// <param name="assigneesJson">The serialized JSON list of assigned approvers (optional).</param>
    /// <returns>A newly created instance of ApprovalRequest.</returns>
    public static ApprovalRequest Create(Guid tenantId, Guid workflowExecutionId, Guid nodeExecutionId, string payloadJson, Guid? requestedBy, int? slaMinutes = null, string? assigneesJson = null)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WorkflowExecutionId = workflowExecutionId,
            NodeExecutionId = nodeExecutionId,
            Status = ApprovalStatus.Pending,
            PayloadJson = payloadJson,
            RequestedBy = requestedBy,
            SlaMinutes = slaMinutes,
            AssigneesJson = assigneesJson,
            RequestedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Approves the approval request with the given responder and comment.
    /// </summary>
    /// <param name="respondedBy">The unique identifier of the user or entity that responded.</param>
    /// <param name="comment">The comment associated with the approval decision (optional).</param>
    public void Approve(Guid respondedBy, string? comment)
    {
        Status = ApprovalStatus.Approved;
        RespondedBy = respondedBy;
        Decision = "approved";
        Comment = comment;
        RespondedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects the approval request with the given responder and comment.
    /// </summary>
    /// <param name="respondedBy">The unique identifier of the user or entity that responded.</param>
    /// <param name="comment">The comment associated with the rejection decision (optional).</param>
    public void Reject(Guid respondedBy, string? comment)
    {
        Status = ApprovalStatus.Rejected;
        RespondedBy = respondedBy;
        Decision = "rejected";
        Comment = comment;
        RespondedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the approval request as expired.
    /// </summary>
    public void Expire()
    {
        Status = ApprovalStatus.Expired;
        RespondedAt = DateTime.UtcNow;
    }
}