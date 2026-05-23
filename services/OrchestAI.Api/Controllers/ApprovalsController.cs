using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestAI.Application.Abstractions;
using OrchestAI.Contracts.Requests;
using OrchestAI.Contracts.Responses;
using OrchestAI.Contracts.Events;
using OrchestAI.Engine;
namespace OrchestAI.Api.Controllers;

[ApiController, Route("api/approvals"), Authorize]
public sealed class ApprovalsController : ControllerBase
{
    private readonly IApprovalRepository _approvals;
    private readonly IExecutionQueue _queue;
    public ApprovalsController(IApprovalRepository approvals, IExecutionQueue queue) { _approvals = approvals; _queue = queue; }
    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());
    private Guid UserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ApprovalRequestResponse>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var items = await _approvals.ListPendingAsync(TenantId, page, pageSize, ct);
        var result = items.Select(a => new ApprovalRequestResponse(a.Id, a.WorkflowExecutionId, a.NodeExecutionId, a.Status.ToString(), a.PayloadJson, a.RequestedAt, a.RespondedAt, a.Decision, a.Comment)).ToList();
        return Ok(new PagedResponse<ApprovalRequestResponse>(result, page, pageSize, result.Count));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApprovalRequestResponse>> Get(Guid id, CancellationToken ct)
    {
        var a = await _approvals.GetAsync(id, TenantId, ct);
        if (a == null) return NotFound();
        return Ok(new ApprovalRequestResponse(a.Id, a.WorkflowExecutionId, a.NodeExecutionId, a.Status.ToString(), a.PayloadJson, a.RequestedAt, a.RespondedAt, a.Decision, a.Comment));
    }

    [HttpPost("{id}/approve")]
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

    [HttpPost("{id}/reject")]
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
