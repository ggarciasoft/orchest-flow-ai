using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestAI.Application.Abstractions;
using OrchestAI.Contracts.Requests;
using OrchestAI.Contracts.Responses;
using OrchestAI.Contracts.Events;
using OrchestAI.Engine;
namespace OrchestAI.Api.Controllers;

/// <summary>
/// Provides endpoints for managing approval requests within the OrchestAI system.
/// All endpoints are scoped to the authenticated user's tenant via the JWT tenant_id claim.
/// </summary>
[ApiController, Route("api/approvals"), Authorize]
public sealed class ApprovalsController : ControllerBase
{
    private readonly IApprovalRepository _approvals;
    private readonly IExecutionQueue _queue;

    /// <summary>Initializes the controller with approval repository and execution queue dependencies.</summary>
    public ApprovalsController(IApprovalRepository approvals, IExecutionQueue queue) { _approvals = approvals; _queue = queue; }

    /// <summary>Extracts the tenant id from the JWT tenant_id claim.</summary>
    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());

    /// <summary>Extracts the user id from the JWT sub claim.</summary>
    private Guid UserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    /// <summary>
    /// Retrieves a paginated list of pending approval requests for the current tenant.
    /// </summary>
    /// <param name="page">Current page number for pagination.</param>
    /// <param name="pageSize">Size of the page for pagination.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A paginated response containing the list of approval requests.</returns>
    /// <response code="200">Paged list of pending approval requests.</response>
    [HttpGet, Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult<PagedResponse<ApprovalRequestResponse>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var items = await _approvals.ListPendingAsync(TenantId, page, pageSize, ct);
        var result = items.Select(a => new ApprovalRequestResponse(a.Id, a.WorkflowExecutionId, a.NodeExecutionId, a.Status.ToString(), a.PayloadJson, a.RequestedAt, a.RespondedAt, a.Decision, a.Comment)).ToList();
        return Ok(new PagedResponse<ApprovalRequestResponse>(result, page, pageSize, result.Count));
    }

    /// <summary>
    /// Retrieves details of a specific approval request by its ID.
    /// </summary>
    /// <param name="id">The ID of the approval request.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The details of the approval request, or a 404 status if not found.</returns>
    /// <response code="200">Approval request found.</response>
    /// <response code="404">Approval request not found or belongs to a different tenant.</response>
    [HttpGet("{id}"), Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult<ApprovalRequestResponse>> Get(Guid id, CancellationToken ct)
    {
        var a = await _approvals.GetAsync(id, TenantId, ct);
        if (a == null) return NotFound();
        return Ok(new ApprovalRequestResponse(a.Id, a.WorkflowExecutionId, a.NodeExecutionId, a.Status.ToString(), a.PayloadJson, a.RequestedAt, a.RespondedAt, a.Decision, a.Comment));
    }

    /// <summary>
    /// Approves a specific approval request and enqueues the decision for execution.
    /// </summary>
    /// <param name="id">The ID of the approval request to approve.</param>
    /// <param name="req">The decision request containing optional comments.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The updated approval request details after approval.</returns>
    /// <response code="200">Approval decision recorded and execution resumed.</response>
    /// <response code="404">Approval request not found.</response>
    [HttpPost("{id}/approve"), Authorize(Policy = "EditorOrAbove")]
    public async Task<ActionResult<ApprovalRequestResponse>> Approve(Guid id, [FromBody] ApprovalDecisionRequest req, CancellationToken ct)
    {
        var a = await _approvals.GetAsync(id, TenantId, ct);
        if (a == null) return NotFound();
        a.Approve(UserId, req.Comment);
        await _approvals.UpdateAsync(a, ct);
        var resumeOutputs = new Dictionary<string, object?> { ["decision"] = "approved", ["comment"] = req.Comment ?? "", ["decidedBy"] = UserId.ToString(), ["decidedAt"] = DateTime.UtcNow.ToString("O") };
        await _queue.EnqueueResumeAsync(new ExecutionResumeMessage(a.WorkflowExecutionId, a.Id, a.NodeExecutionId, resumeOutputs), ct);
        return Ok(new ApprovalRequestResponse(a.Id, a.WorkflowExecutionId, a.NodeExecutionId, a.Status.ToString(), a.PayloadJson, a.RequestedAt, a.RespondedAt, a.Decision, a.Comment));
    }

    /// <summary>
    /// Rejects a specific approval request and enqueues the decision for execution.
    /// </summary>
    /// <param name="id">The ID of the approval request to reject.</param>
    /// <param name="req">The decision request containing optional comments.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The updated approval request details after rejection.</returns>
    /// <response code="200">Rejection decision recorded and execution resumed.</response>
    /// <response code="404">Approval request not found.</response>
    [HttpPost("{id}/reject"), Authorize(Policy = "EditorOrAbove")]
    public async Task<ActionResult<ApprovalRequestResponse>> Reject(Guid id, [FromBody] ApprovalDecisionRequest req, CancellationToken ct)
    {
        var a = await _approvals.GetAsync(id, TenantId, ct);
        if (a == null) return NotFound();
        a.Reject(UserId, req.Comment);
        await _approvals.UpdateAsync(a, ct);
        var resumeOutputs = new Dictionary<string, object?> { ["decision"] = "rejected", ["comment"] = req.Comment ?? "", ["decidedBy"] = UserId.ToString(), ["decidedAt"] = DateTime.UtcNow.ToString("O") };
        await _queue.EnqueueResumeAsync(new ExecutionResumeMessage(a.WorkflowExecutionId, a.Id, a.NodeExecutionId, resumeOutputs), ct);
        return Ok(new ApprovalRequestResponse(a.Id, a.WorkflowExecutionId, a.NodeExecutionId, a.Status.ToString(), a.PayloadJson, a.RequestedAt, a.RespondedAt, a.Decision, a.Comment));
    }
}
