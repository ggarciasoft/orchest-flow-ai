using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestAI.Application.Abstractions;
using OrchestAI.Contracts.Events;
using OrchestAI.Contracts.Requests;
using OrchestAI.Contracts.Responses;
using OrchestAI.Domain.Entities;
using OrchestAI.Engine;
using OrchestAI.Engine.Registry;
using System.Text.Json;
namespace OrchestAI.Api.Controllers;

/// <summary>
/// Provides endpoints for managing workflow executions within OrchestAI.
/// </summary>
[ApiController, Route("api/executions"), Authorize]
/// <summary>
/// Controller for managing executions of workflows. Supports listing workflow executions,
/// retrieving details, and obtaining the execution timeline.
/// </summary>
public sealed class ExecutionsController : ControllerBase
{
    private readonly IExecutionRepository _executions;
    public ExecutionsController(IExecutionRepository executions) => _executions = executions;
    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    /// <summary>
    /// Retrieves a paginated list of workflow executions for the current tenant,
    /// with optional filtering by status.
    /// </summary>
    /// <param name="status">The status to filter workflow executions by (optional).</param>
    /// <param name="page">Current page number for pagination.</param>
    /// <param name="pageSize">Size of the page for pagination.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A paginated response containing the list of workflow executions.</returns>
    public async Task<ActionResult<PagedResponse<WorkflowExecutionResponse>>> List([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var items = await _executions.ListAsync(TenantId, status, page, pageSize, ct);
        var result = items.Select(e => new WorkflowExecutionResponse(e.Id, e.WorkflowId, e.WorkflowVersionId, e.Status.ToString(), e.StartedAt, e.CompletedAt, e.TriggeredBy, e.CorrelationId, e.ErrorMessage)).ToList();
        return Ok(new PagedResponse<WorkflowExecutionResponse>(result, page, pageSize, result.Count));
    }

    [HttpGet("{id}")]
    /// <summary>
    /// Retrieves details of a specific workflow execution by its ID.
    /// </summary>
    /// <param name="id">The ID of the workflow execution.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The details of the workflow execution, or a 404 status code if not found.</returns>
    public async Task<ActionResult<WorkflowExecutionResponse>> Get(Guid id, CancellationToken ct)
    {
        var e = await _executions.GetAsync(id, ct);
        if (e == null || e.TenantId != TenantId) return NotFound();
        return Ok(new WorkflowExecutionResponse(e.Id, e.WorkflowId, e.WorkflowVersionId, e.Status.ToString(), e.StartedAt, e.CompletedAt, e.TriggeredBy, e.CorrelationId, e.ErrorMessage));
    }

    [HttpGet("{id}/timeline")]
    /// <summary>
    /// Retrieves the timeline details of a specific workflow execution, including nodes executed.
    /// </summary>
    /// <param name="id">The ID of the workflow execution.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The timeline of the workflow execution, or a 404 status code if not found.</returns>
    public async Task<ActionResult<ExecutionTimelineResponse>> Timeline(Guid id, CancellationToken ct)
    {
        var e = await _executions.GetAsync(id, ct);
        if (e == null || e.TenantId != TenantId) return NotFound();
        var nodes = await _executions.GetNodeExecutionsAsync(id, ct);
        var nodeResponses = nodes.Select(n => new NodeExecutionResponse(n.Id, n.WorkflowExecutionId, n.NodeId, n.NodeType, n.Status.ToString(), n.StartedAt, n.CompletedAt, n.InputJson, n.OutputJson, n.ErrorMessage, n.RetryCount, n.Step)).ToList();
        return Ok(new ExecutionTimelineResponse(id, nodeResponses));
    }
}
