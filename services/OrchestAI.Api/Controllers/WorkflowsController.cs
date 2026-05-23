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

[ApiController, Route("api/workflows"), Authorize]
public sealed class WorkflowsController : ControllerBase
{
    private readonly IWorkflowRepository _workflows;
    private readonly IExecutionRepository _executions;
    private readonly IExecutionQueue _queue;
    private readonly IWorkflowEngine _engine;
    private readonly INodeRegistry _registry;

    public WorkflowsController(IWorkflowRepository workflows, IExecutionRepository executions, IExecutionQueue queue, IWorkflowEngine engine, INodeRegistry registry)
    { _workflows = workflows; _executions = executions; _queue = queue; _engine = engine; _registry = registry; }

    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());
    private Guid UserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<PagedResponse<WorkflowResponse>>> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var items = await _workflows.ListAsync(TenantId, search, page, pageSize, ct);
        var total = await _workflows.CountAsync(TenantId, search, ct);
        var result = items.Select(w => new WorkflowResponse(w.Id, w.Name, w.Description, null, w.CreatedAt, w.UpdatedAt)).ToList();
        return Ok(new PagedResponse<WorkflowResponse>(result, page, pageSize, total));
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowResponse>> Create([FromBody] CreateWorkflowRequest req, CancellationToken ct)
    {
        var workflow = Workflow.Create(TenantId, req.Name, req.Description, UserId);
        await _workflows.CreateAsync(workflow, ct);
        var defJson = req.Definition.GetRawText();
        var version = WorkflowVersion.Create(workflow.Id, 1, defJson, UserId);
        version.Activate();
        await _workflows.CreateVersionAsync(version, ct);
        return CreatedAtAction(nameof(Get), new { id = workflow.Id }, new WorkflowResponse(workflow.Id, workflow.Name, workflow.Description, 1, workflow.CreatedAt, workflow.UpdatedAt));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowResponse>> Get(Guid id, CancellationToken ct)
    {
        var w = await _workflows.GetAsync(id, TenantId, ct);
        if (w == null) return NotFound();
        var activeVersion = await _workflows.GetActiveVersionAsync(id, ct);
        return Ok(new WorkflowResponse(w.Id, w.Name, w.Description, activeVersion?.VersionNumber, w.CreatedAt, w.UpdatedAt));
    }

    [HttpPost("{id}/execute")]
    public async Task<ActionResult<WorkflowExecutionResponse>> Execute(Guid id, [FromBody] ExecuteWorkflowRequest req, CancellationToken ct)
    {
        var workflow = await _workflows.GetAsync(id, TenantId, ct);
        if (workflow == null) return NotFound();
        var activeVersion = await _workflows.GetActiveVersionAsync(id, ct);
        if (activeVersion == null) return BadRequest("Workflow has no active version");
        var inputJson = req.Input != null ? JsonSerializer.Serialize(req.Input) : "{}";
        var correlationId = Guid.NewGuid().ToString();
        var execution = WorkflowExecution.Create(TenantId, id, activeVersion.Id, UserId, inputJson, correlationId);
        await _executions.CreateAsync(execution, ct);
        await _queue.EnqueueAsync(new ExecutionQueueMessage(execution.Id, TenantId, correlationId), ct);
        return Accepted(new WorkflowExecutionResponse(execution.Id, id, activeVersion.Id, execution.Status.ToString(), execution.StartedAt, null, UserId, correlationId, null));
    }

    [HttpPost("{id}/validate")]
    public async Task<ActionResult> Validate(Guid id, CancellationToken ct)
    {
        var activeVersion = await _workflows.GetActiveVersionAsync(id, ct);
        if (activeVersion == null) return BadRequest("No active version");
        var def = JsonSerializer.Deserialize<OrchestAI.Engine.Models.WorkflowDefinition>(activeVersion.DefinitionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (def == null) return BadRequest("Invalid definition JSON");
        var result = await _engine.ValidateAsync(def, ct);
        return Ok(new { isValid = result.IsValid, errors = result.Errors });
    }

    [HttpPost("{id}/versions")]
    public async Task<ActionResult> CreateVersion(Guid id, [FromBody] CreateWorkflowVersionRequest req, CancellationToken ct)
    {
        var workflow = await _workflows.GetAsync(id, TenantId, ct);
        if (workflow == null) return NotFound();
        var activeVersion = await _workflows.GetActiveVersionAsync(id, ct);
        var nextVersionNumber = (activeVersion?.VersionNumber ?? 0) + 1;
        var version = WorkflowVersion.Create(id, nextVersionNumber, req.Definition.GetRawText(), UserId);
        await _workflows.CreateVersionAsync(version, ct);
        return Ok(new { id = version.Id, versionNumber = version.VersionNumber });
    }

    [HttpPost("{id}/versions/{versionId}/activate")]
    public async Task<ActionResult> ActivateVersion(Guid id, Guid versionId, CancellationToken ct)
    {
        await _workflows.ActivateVersionAsync(versionId, id, ct);
        return Ok();
    }
}
