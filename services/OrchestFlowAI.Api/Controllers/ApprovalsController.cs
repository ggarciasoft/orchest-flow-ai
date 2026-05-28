using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Requests;
using OrchestFlowAI.Contracts.Responses;
using OrchestFlowAI.Contracts.Events;
using OrchestFlowAI.Engine;
using OrchestFlowAI.Domain.Entities;
using System.Text.Json;

namespace OrchestFlowAI.Api.Controllers;

/// <summary>
/// Provides endpoints for managing approval requests within the OrchestFlowAI system.
/// All endpoints are scoped to the authenticated user's tenant via the JWT tenant_id claim.
/// </summary>
[ApiController, Route("api/approvals"), Authorize]
public sealed class ApprovalsController : ControllerBase
{
    private readonly IApprovalRepository _approvals;
    private readonly IExecutionQueue _queue;
    private readonly IExecutionRepository _executions;
    private readonly IWorkflowRepository _workflows;
    private readonly IFormRepository _forms;

    public ApprovalsController(
        IApprovalRepository approvals,
        IExecutionQueue queue,
        IExecutionRepository executions,
        IWorkflowRepository workflows,
        IFormRepository forms)
    {
        _approvals = approvals;
        _queue = queue;
        _executions = executions;
        _workflows = workflows;
        _forms = forms;
    }

    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());
    private Guid UserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    /// <summary>Enriches an approval response with workflow/form context from related entities.</summary>
    private async Task<ApprovalRequestResponse> EnrichAsync(ApprovalRequest a, CancellationToken ct)
    {
        string? workflowName = null;
        Guid? workflowId = null;
        int? workflowVersionNumber = null;
        int? formVersionNumber = null;

        // Load execution to get WorkflowId + WorkflowVersionId
        var execution = await _executions.GetAsync(a.WorkflowExecutionId, ct);
        if (execution != null)
        {
            workflowId = execution.WorkflowId;
            var workflow = await _workflows.GetByIdAsync(execution.WorkflowId, ct);
            workflowName = workflow?.Name;

            // Look up version number from the version used in this execution
            var version = await _workflows.GetVersionAsync(execution.WorkflowVersionId, ct);
            workflowVersionNumber = version?.VersionNumber;
        }

        // If this is a form approval, find the active form version number
        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(a.PayloadJson);
            if (payload != null && payload.TryGetValue("_formId", out var formIdEl))
            {
                if (Guid.TryParse(formIdEl.GetString(), out var formId))
                {
                    var activeVersion = await _forms.GetActiveVersionAsync(formId, ct);
                    formVersionNumber = activeVersion?.VersionNumber;
                }
            }
        }
        catch { /* ignore - enrichment is best-effort */ }

        return new ApprovalRequestResponse(
            a.Id, a.WorkflowExecutionId, a.NodeExecutionId,
            a.Status.ToString(), a.PayloadJson,
            a.RequestedAt, a.RespondedAt, a.Decision, a.Comment,
            WorkflowName: workflowName,
            WorkflowId: workflowId,
            WorkflowVersionNumber: workflowVersionNumber,
            FormVersionNumber: formVersionNumber);
    }

    /// <summary>Retrieves a paginated list of pending approval requests for the current tenant.</summary>
    [HttpGet, Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult<PagedResponse<ApprovalRequestResponse>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var items = await _approvals.ListPendingAsync(TenantId, page, pageSize, ct);
        var result = await Task.WhenAll(items.Select(a => EnrichAsync(a, ct)));
        return Ok(new PagedResponse<ApprovalRequestResponse>(result.ToList(), page, pageSize, result.Length));
    }

    /// <summary>Retrieves details of a specific approval request by its ID.</summary>
    [HttpGet("{id}"), Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult<ApprovalRequestResponse>> Get(Guid id, CancellationToken ct)
    {
        var a = await _approvals.GetAsync(id, TenantId, ct);
        if (a == null) return NotFound();
        return Ok(await EnrichAsync(a, ct));
    }

    /// <summary>Approves a specific approval request and enqueues the decision for execution.</summary>
    [HttpPost("{id}/approve"), Authorize(Policy = "EditorOrAbove")]
    public async Task<ActionResult<ApprovalRequestResponse>> Approve(Guid id, [FromBody] ApprovalDecisionRequest req, CancellationToken ct)
    {
        var a = await _approvals.GetAsync(id, TenantId, ct);
        if (a == null) return NotFound();
        a.Approve(UserId, req.Comment);
        await _approvals.UpdateAsync(a, ct);
        var resumeOutputs = new Dictionary<string, object?> { ["decision"] = "approved", ["comment"] = req.Comment ?? "", ["decidedBy"] = UserId.ToString(), ["decidedAt"] = DateTime.UtcNow.ToString("O") };
        await _queue.EnqueueResumeAsync(new ExecutionResumeMessage(a.WorkflowExecutionId, a.Id, a.NodeExecutionId, resumeOutputs), ct);
        return Ok(await EnrichAsync(a, ct));
    }

    /// <summary>Rejects a specific approval request and enqueues the decision for execution.</summary>
    [HttpPost("{id}/reject"), Authorize(Policy = "EditorOrAbove")]
    public async Task<ActionResult<ApprovalRequestResponse>> Reject(Guid id, [FromBody] ApprovalDecisionRequest req, CancellationToken ct)
    {
        var a = await _approvals.GetAsync(id, TenantId, ct);
        if (a == null) return NotFound();
        a.Reject(UserId, req.Comment);
        await _approvals.UpdateAsync(a, ct);
        var resumeOutputs = new Dictionary<string, object?> { ["decision"] = "rejected", ["comment"] = req.Comment ?? "", ["decidedBy"] = UserId.ToString(), ["decidedAt"] = DateTime.UtcNow.ToString("O") };
        await _queue.EnqueueResumeAsync(new ExecutionResumeMessage(a.WorkflowExecutionId, a.Id, a.NodeExecutionId, resumeOutputs), ct);
        return Ok(await EnrichAsync(a, ct));
    }
}
