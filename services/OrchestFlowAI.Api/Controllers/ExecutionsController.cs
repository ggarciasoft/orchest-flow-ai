using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Events;
using OrchestFlowAI.Contracts.Requests;
using OrchestFlowAI.Contracts.Responses;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Engine;
using OrchestFlowAI.Engine.Registry;
using System.Text.Json;
namespace OrchestFlowAI.Api.Controllers;

/// <summary>
/// Provides endpoints for managing workflow executions within OrchestFlowAI.
/// All endpoints are scoped to the authenticated user's tenant via the JWT tenant_id claim.
/// </summary>
[ApiController, Route("api/executions"), Authorize]
public sealed class ExecutionsController : ControllerBase
{
    private readonly IExecutionRepository _executions;
    private readonly IWorkflowRepository _workflows;
    private readonly IWorkflowEngine _engine;
    private readonly IExecutionQueue _queue;

    /// <summary>Initializes the controller with execution and workflow repository dependencies.</summary>
    public ExecutionsController(IExecutionRepository executions, IWorkflowRepository workflows, IWorkflowEngine engine, IExecutionQueue queue)
    { _executions = executions; _workflows = workflows; _engine = engine; _queue = queue; }

    /// <summary>Extracts the tenant id from the JWT tenant_id claim.</summary>
    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());

    /// <summary>
    /// Retrieves a paginated list of workflow executions for the current tenant,
    /// with optional filtering by status.
    /// </summary>
    /// <param name="status">The status to filter workflow executions by (optional).</param>
    /// <param name="page">Current page number for pagination.</param>
    /// <param name="pageSize">Size of the page for pagination.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A paginated response containing the list of workflow executions.</returns>
    /// <response code="200">Paged list of executions.</response>
    [HttpGet, Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult<PagedResponse<WorkflowExecutionResponse>>> List([FromQuery] string? status, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var items = await _executions.ListAsync(TenantId, status, search, page, pageSize, ct);
        var total = await _executions.CountAsync(TenantId, status, search, ct);

        // Batch-fetch distinct workflow metadata to enrich each execution row
        var workflowIds = items.Select(e => e.WorkflowId).Distinct().ToList();
        var workflowMap = new Dictionary<Guid, Workflow>();
        foreach (var wid in workflowIds)
        {
            var w = await _workflows.GetAsync(wid, TenantId, ct);
            if (w != null) workflowMap[wid] = w;
        }

        // Batch-fetch distinct version metadata
        var versionIds = items.Select(e => e.WorkflowVersionId).Distinct().ToList();
        var versionMap = new Dictionary<Guid, int>();
        foreach (var vid in versionIds)
        {
            var v = await _workflows.GetVersionAsync(vid, ct);
            if (v != null) versionMap[vid] = v.VersionNumber;
        }

        var result = items.Select(e => new WorkflowExecutionResponse(
            e.Id, e.WorkflowId, e.WorkflowVersionId, e.Status.ToString(),
            e.StartedAt, e.CompletedAt, e.TriggeredBy, e.CorrelationId, e.ErrorMessage,
            workflowMap.TryGetValue(e.WorkflowId, out var wf) ? wf.Name : null,
            versionMap.TryGetValue(e.WorkflowVersionId, out var vn) ? vn : null
        )).ToList();

        return Ok(new PagedResponse<WorkflowExecutionResponse>(result, page, pageSize, total));
    }

    /// <summary>
    /// Retrieves details of a specific workflow execution by its ID.
    /// </summary>
    /// <param name="id">The ID of the workflow execution.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The details of the workflow execution, or a 404 status code if not found.</returns>
    /// <response code="200">Execution found.</response>
    /// <response code="404">Execution not found or belongs to a different tenant.</response>
    [HttpGet("{id}"), Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult<WorkflowExecutionResponse>> Get(Guid id, CancellationToken ct)
    {
        var e = await _executions.GetAsync(id, ct);
        if (e == null || e.TenantId != TenantId) return NotFound();
        var wf = await _workflows.GetAsync(e.WorkflowId, TenantId, ct);
        var version = await _workflows.GetVersionAsync(e.WorkflowVersionId, ct);
        return Ok(new WorkflowExecutionResponse(
            e.Id, e.WorkflowId, e.WorkflowVersionId, e.Status.ToString(),
            e.StartedAt, e.CompletedAt, e.TriggeredBy, e.CorrelationId, e.ErrorMessage,
            wf?.Name, version?.VersionNumber));
    }

    /// <summary>
    /// Retrieves the timeline details of a specific workflow execution, including nodes executed.
    /// </summary>
    /// <param name="id">The ID of the workflow execution.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The timeline of the workflow execution, or a 404 status code if not found.</returns>
    /// <response code="200">Execution timeline found.</response>
    /// <response code="404">Execution not found or belongs to a different tenant.</response>
    [HttpGet("{id}/timeline"), Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult<ExecutionTimelineResponse>> Timeline(Guid id, CancellationToken ct)
    {
        var e = await _executions.GetAsync(id, ct);
        if (e == null || e.TenantId != TenantId) return NotFound();
        var nodes = await _executions.GetNodeExecutionsAsync(id, ct);
        var nodeResponses = nodes.Select(n =>
        {
            string? corrToken = null;
            string? resumeUrl = null;
            if (n.OutputJson != null)
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(n.OutputJson);
                    if (doc.RootElement.TryGetProperty("_correlationToken", out var ctEl))
                    {
                        corrToken = ctEl.GetString();
                        resumeUrl = $"/api/webhooks/resume/{corrToken}";
                    }
                }
                catch { /* ignore malformed JSON */ }
            }
            return new NodeExecutionResponse(n.Id, n.WorkflowExecutionId, n.NodeId, n.NodeType, n.Status.ToString(), n.StartedAt, n.CompletedAt, n.InputJson, n.OutputJson, n.ErrorMessage, n.RetryCount, n.Step, corrToken, resumeUrl);
        }).ToList();
        return Ok(new ExecutionTimelineResponse(id, nodeResponses));
    }

    /// <summary>
    /// Re-runs an execution using the same workflow version and inputs.
    /// Creates a fresh execution — does not modify the original.
    /// </summary>
    /// <param name="id">The ID of the execution to re-run.</param>
    /// <response code="202">New execution enqueued.</response>
    /// <response code="404">Original execution not found.</response>
    [HttpPost("{id}/rerun"), Authorize(Policy = "EditorOrAbove")]
    public async Task<ActionResult<WorkflowExecutionResponse>> Rerun(Guid id, CancellationToken ct)
    {
        var original = await _executions.GetAsync(id, ct);
        if (original == null || original.TenantId != TenantId) return NotFound();

        var correlationId = Guid.NewGuid().ToString();
        var execution = WorkflowExecution.Create(
            original.TenantId, original.WorkflowId, original.WorkflowVersionId,
            original.TriggeredBy, original.InputJson, correlationId);

        await _executions.CreateAsync(execution, ct);
        await _queue.EnqueueAsync(new OrchestFlowAI.Contracts.Events.ExecutionQueueMessage(execution.Id, original.TenantId, correlationId), ct);

        var wf = await _workflows.GetAsync(original.WorkflowId, TenantId, ct);
        var version = await _workflows.GetVersionAsync(original.WorkflowVersionId, ct);
        return Accepted(new WorkflowExecutionResponse(
            execution.Id, execution.WorkflowId, execution.WorkflowVersionId,
            execution.Status.ToString(), execution.StartedAt, null,
            execution.TriggeredBy, correlationId, null,
            wf?.Name, version?.VersionNumber));
    }

    /// <summary>
    /// Cancels a running, queued, or paused workflow execution.
    /// </summary>
    /// <param name="id">The ID of the workflow execution to cancel.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>204 NoContent on success; 404 if not found; 409 if already in terminal state.</returns>
    /// <response code="204">Execution successfully cancelled.</response>
    /// <response code="404">Execution not found or belongs to a different tenant.</response>
    /// <response code="409">Execution is already in a terminal state (Completed, Failed, or Cancelled).</response>
    [HttpPost("{id}/cancel"), Authorize(Policy = "EditorOrAbove")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var e = await _executions.GetAsync(id, ct);
        if (e == null || e.TenantId != TenantId) return NotFound();
        try
        {
            await _engine.CancelAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}
