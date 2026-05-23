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

[ApiController, Route("api/executions"), Authorize]
public sealed class ExecutionsController : ControllerBase
{
    private readonly IExecutionRepository _executions;
    public ExecutionsController(IExecutionRepository executions) => _executions = executions;
    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<PagedResponse<WorkflowExecutionResponse>>> List([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var items = await _executions.ListAsync(TenantId, status, page, pageSize, ct);
        var result = items.Select(e => new WorkflowExecutionResponse(e.Id, e.WorkflowId, e.WorkflowVersionId, e.Status.ToString(), e.StartedAt, e.CompletedAt, e.TriggeredBy, e.CorrelationId, e.ErrorMessage)).ToList();
        return Ok(new PagedResponse<WorkflowExecutionResponse>(result, page, pageSize, result.Count));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowExecutionResponse>> Get(Guid id, CancellationToken ct)
    {
        var e = await _executions.GetAsync(id, ct);
        if (e == null || e.TenantId != TenantId) return NotFound();
        return Ok(new WorkflowExecutionResponse(e.Id, e.WorkflowId, e.WorkflowVersionId, e.Status.ToString(), e.StartedAt, e.CompletedAt, e.TriggeredBy, e.CorrelationId, e.ErrorMessage));
    }

    [HttpGet("{id}/timeline")]
    public async Task<ActionResult<ExecutionTimelineResponse>> Timeline(Guid id, CancellationToken ct)
    {
        var e = await _executions.GetAsync(id, ct);
        if (e == null || e.TenantId != TenantId) return NotFound();
        var nodes = await _executions.GetNodeExecutionsAsync(id, ct);
        var nodeResponses = nodes.Select(n => new NodeExecutionResponse(n.Id, n.WorkflowExecutionId, n.NodeId, n.NodeType, n.Status.ToString(), n.StartedAt, n.CompletedAt, n.InputJson, n.OutputJson, n.ErrorMessage, n.RetryCount, n.Step)).ToList();
        return Ok(new ExecutionTimelineResponse(id, nodeResponses));
    }
}
