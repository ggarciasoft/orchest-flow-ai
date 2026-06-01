using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Requests;
using OrchestFlowAI.Contracts.Responses;
using OrchestFlowAI.Contracts.Events;
using OrchestFlowAI.Engine;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
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
    private readonly IUserRepository _users;

    public ApprovalsController(
        IApprovalRepository approvals,
        IExecutionQueue queue,
        IExecutionRepository executions,
        IWorkflowRepository workflows,
        IUserRepository users)
    {
        _approvals = approvals;
        _queue = queue;
        _executions = executions;
        _workflows = workflows;
        _users = users;
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

        // If this is a form approval, read the version number baked into the payload at execution time
        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(a.PayloadJson);
            if (payload != null && payload.TryGetValue("_formVersionNumber", out var fvEl))
                formVersionNumber = fvEl.ValueKind == JsonValueKind.Number ? fvEl.GetInt32() : null;
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
        var result = new List<ApprovalRequestResponse>(items.Count);
        foreach (var a in items)
            result.Add(await EnrichAsync(a, ct));
        return Ok(new PagedResponse<ApprovalRequestResponse>(result, page, pageSize, result.Count));
    }

    /// <summary>Retrieves the pending approval request for a given workflow execution, if any.</summary>
    [HttpGet("by-execution/{executionId}"), Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult<ApprovalRequestResponse>> GetByExecution(Guid executionId, CancellationToken ct)
    {
        var a = await _approvals.GetByExecutionIdAsync(executionId, TenantId, ct);
        if (a == null) return NotFound();
        return Ok(await EnrichAsync(a, ct));
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

    /// <summary>Submits a document selection for a document-selection approval request and resumes the workflow.</summary>
    [HttpPost("{id}/select-document"), Authorize(Policy = "EditorOrAbove")]
    public async Task<ActionResult<ApprovalRequestResponse>> SelectDocument(Guid id, [FromBody] SelectDocumentRequest req, CancellationToken ct)
    {
        var a = await _approvals.GetAsync(id, TenantId, ct);
        if (a == null) return NotFound();
        if (a.Status != ApprovalStatus.Pending) return BadRequest("Approval is not pending.");
        a.Approve(UserId, null);
        await _approvals.UpdateAsync(a, ct);
        var resumeOutputs = new Dictionary<string, object?>
        {
            ["documentId"] = req.DocumentId.ToString(),
            ["filename"] = req.Filename,
            ["mimeType"] = req.MimeType,
            ["sizeBytes"] = (object?)req.SizeBytes,
            ["sha256"] = req.Sha256,
            ["selectedBy"] = UserId.ToString(),
            ["selectedAt"] = DateTime.UtcNow.ToString("O"),
        };
        await _queue.EnqueueResumeAsync(new ExecutionResumeMessage(a.WorkflowExecutionId, a.Id, a.NodeExecutionId, resumeOutputs), ct);
        return Ok(await EnrichAsync(a, ct));
    }

    /// <summary>Selects a document for an approval request and resumes execution with the document metadata.</summary>
    [HttpPost("{id}/select-document"), Authorize(Policy = "EditorOrAbove")]
    public async Task<ActionResult<ApprovalRequestResponse>> SelectDocument(Guid id, [FromBody] SelectDocumentRequest req, CancellationToken ct)
    {
        var a = await _approvals.GetAsync(id, TenantId, ct);
        if (a == null) return NotFound();
        if (a.Status != ApprovalStatus.Pending) return BadRequest("Approval is not pending.");
        a.Approve(UserId, null);
        await _approvals.UpdateAsync(a, ct);
        var resumeOutputs = new Dictionary<string, object?>
        {
            ["documentId"] = req.DocumentId.ToString(),
            ["filename"] = req.Filename,
            ["mimeType"] = req.MimeType,
            ["sizeBytes"] = (object?)req.SizeBytes,
            ["sha256"] = req.Sha256,
            ["selectedBy"] = UserId.ToString(),
            ["selectedAt"] = DateTime.UtcNow.ToString("O"),
        };
        await _queue.EnqueueResumeAsync(new ExecutionResumeMessage(a.WorkflowExecutionId, a.Id, a.NodeExecutionId, resumeOutputs), ct);
        return Ok(await EnrichAsync(a, ct));
    }

    /// <summary>Lists all comments on an approval request, oldest first.</summary>
    [HttpGet("{id}/comments"), Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult> ListComments(Guid id, CancellationToken ct)
    {
        var a = await _approvals.GetAsync(id, TenantId, ct);
        if (a == null) return NotFound();
        var comments = await _approvals.ListCommentsAsync(id, ct);
        return Ok(comments.Select(c => new
        {
            id = c.Id,
            authorId = c.AuthorId,
            authorName = c.AuthorName,
            text = c.Text,
            createdAt = c.CreatedAt,
        }));
    }

    /// <summary>Posts a comment on an approval request.</summary>
    [HttpPost("{id}/comments"), Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult> AddComment(Guid id, [FromBody] AddApprovalCommentRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Text)) return BadRequest("Comment text is required.");
        var a = await _approvals.GetAsync(id, TenantId, ct);
        if (a == null) return NotFound();

        // Resolve display name — fall back to user id if not found
        var user = await _users.GetAsync(UserId, TenantId, ct);
        var authorName = user?.DisplayName ?? UserId.ToString();

        var comment = ApprovalComment.Create(id, UserId, authorName, req.Text);
        await _approvals.AddCommentAsync(comment, ct);

        return StatusCode(201, new
        {
            id = comment.Id,
            authorId = comment.AuthorId,
            authorName = comment.AuthorName,
            text = comment.Text,
            createdAt = comment.CreatedAt,
        });
    }
}
