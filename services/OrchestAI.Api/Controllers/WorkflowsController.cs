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
/// Manages workflow definitions, versions, validation, and execution triggering.
/// All endpoints are scoped to the authenticated user's tenant via the JWT tenant_id claim.
/// </summary>
[ApiController, Route("api/workflows"), Authorize]
public sealed class WorkflowsController : ControllerBase
{
    private readonly IWorkflowRepository _workflows;
    private readonly IExecutionRepository _executions;
    private readonly IExecutionQueue _queue;
    private readonly IWorkflowEngine _engine;
    private readonly INodeRegistry _registry;

    /// <summary>Initializes the controller with required workflow, execution, and engine dependencies.</summary>
    public WorkflowsController(IWorkflowRepository workflows, IExecutionRepository executions, IExecutionQueue queue, IWorkflowEngine engine, INodeRegistry registry)
    { _workflows = workflows; _executions = executions; _queue = queue; _engine = engine; _registry = registry; }

    /// <summary>Extracts the tenant id from the JWT tenant_id claim.</summary>
    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());

    /// <summary>Extracts the user id from the JWT sub claim.</summary>
    private Guid UserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    /// <summary>
    /// Lists all workflows for the current tenant with optional search and pagination.
    /// </summary>
    /// <param name="search">Optional name filter (case-insensitive contains match).</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Maximum results per page.</param>
    /// <response code="200">Paged list of workflows.</response>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<WorkflowResponse>>> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var items = await _workflows.ListAsync(TenantId, search, page, pageSize, ct);
        var total = await _workflows.CountAsync(TenantId, search, ct);
        var result = items.Select(w => new WorkflowResponse(w.Id, w.Name, w.Description, null, w.CreatedAt, w.UpdatedAt)).ToList();
        return Ok(new PagedResponse<WorkflowResponse>(result, page, pageSize, total));
    }

    /// <summary>
    /// Creates a new workflow with an initial version containing the provided definition.
    /// The first version is automatically activated.
    /// </summary>
    /// <param name="req">Workflow name, description, and initial node/edge definition.</param>
    /// <response code="201">Workflow created and initial version activated.</response>
    /// <response code="400">Invalid request body.</response>
    [HttpPost]
    public async Task<ActionResult<WorkflowResponse>> Create([FromBody] CreateWorkflowRequest req, CancellationToken ct)
    {
        var workflow = Workflow.Create(TenantId, req.Name, req.Description, UserId);
        await _workflows.CreateAsync(workflow, ct);
        var defJson = req.Definition.GetRawText();
        // Version 1 is created and immediately activated for new workflows
        var version = WorkflowVersion.Create(workflow.Id, 1, defJson, UserId);
        version.Activate();
        await _workflows.CreateVersionAsync(version, ct);
        return CreatedAtAction(nameof(Get), new { id = workflow.Id }, new WorkflowResponse(workflow.Id, workflow.Name, workflow.Description, 1, workflow.CreatedAt, workflow.UpdatedAt));
    }

    /// <summary>
    /// Gets a single workflow by id, including its active version number.
    /// </summary>
    /// <param name="id">The workflow id.</param>
    /// <response code="200">Workflow found.</response>
    /// <response code="404">Workflow not found or belongs to a different tenant.</response>
    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowResponse>> Get(Guid id, CancellationToken ct)
    {
        var w = await _workflows.GetAsync(id, TenantId, ct);
        if (w == null) return NotFound();
        var activeVersion = await _workflows.GetActiveVersionAsync(id, ct);
        return Ok(new WorkflowResponse(w.Id, w.Name, w.Description, activeVersion?.VersionNumber, w.CreatedAt, w.UpdatedAt));
    }

    /// <summary>
    /// Enqueues a workflow execution using the currently active version.
    /// Returns 202 Accepted immediately — execution runs asynchronously via the worker.
    /// </summary>
    /// <param name="id">The workflow id to execute.</param>
    /// <param name="req">Optional input payload to pass to the workflow's start node.</param>
    /// <response code="202">Execution enqueued successfully.</response>
    /// <response code="400">Workflow has no active version.</response>
    /// <response code="404">Workflow not found.</response>
    [HttpPost("{id}/execute")]
    public async Task<ActionResult<WorkflowExecutionResponse>> Execute(Guid id, [FromBody] ExecuteWorkflowRequest req, CancellationToken ct)
    {
        var workflow = await _workflows.GetAsync(id, TenantId, ct);
        if (workflow == null) return NotFound();
        var activeVersion = await _workflows.GetActiveVersionAsync(id, ct);
        if (activeVersion == null) return BadRequest("Workflow has no active version");
        var inputJson = req.Input != null ? JsonSerializer.Serialize(req.Input) : "{}";
        // Correlation id links all logs and node executions for this run
        var correlationId = Guid.NewGuid().ToString();
        var execution = WorkflowExecution.Create(TenantId, id, activeVersion.Id, UserId, inputJson, correlationId);
        await _executions.CreateAsync(execution, ct);
        await _queue.EnqueueAsync(new ExecutionQueueMessage(execution.Id, TenantId, correlationId), ct);
        return Accepted(new WorkflowExecutionResponse(execution.Id, id, activeVersion.Id, execution.Status.ToString(), execution.StartedAt, null, UserId, correlationId, null));
    }

    /// <summary>
    /// Validates the active version's workflow definition without executing it.
    /// Checks for structural issues: missing start/end nodes, unknown types, dangling edges.
    /// </summary>
    /// <param name="id">The workflow id to validate.</param>
    /// <response code="200">Validation result with isValid flag and error list.</response>
    /// <response code="400">No active version or malformed definition JSON.</response>
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

    /// <summary>
    /// Saves a new version of the workflow definition.
    /// Does NOT automatically activate — call the activate endpoint separately.
    /// </summary>
    /// <param name="id">The workflow id.</param>
    /// <param name="req">The new workflow definition (nodes + edges).</param>
    /// <response code="200">New version created with its id and version number.</response>
    /// <response code="404">Workflow not found.</response>
    [HttpPost("{id}/versions")]
    public async Task<ActionResult> CreateVersion(Guid id, [FromBody] CreateWorkflowVersionRequest req, CancellationToken ct)
    {
        var workflow = await _workflows.GetAsync(id, TenantId, ct);
        if (workflow == null) return NotFound();
        var activeVersion = await _workflows.GetActiveVersionAsync(id, ct);
        // Increment version number from the current active version (or start at 1 if none)
        var nextVersionNumber = (activeVersion?.VersionNumber ?? 0) + 1;
        var version = WorkflowVersion.Create(id, nextVersionNumber, req.Definition.GetRawText(), UserId);
        await _workflows.CreateVersionAsync(version, ct);
        return Ok(new { id = version.Id, versionNumber = version.VersionNumber });
    }

    /// <summary>
    /// Activates a specific workflow version, deactivating all others for this workflow.
    /// </summary>
    /// <param name="id">The workflow id.</param>
    /// <param name="versionId">The version id to activate.</param>
    /// <response code="200">Version activated successfully.</response>
    [HttpPost("{id}/versions/{versionId}/activate")]
    public async Task<ActionResult> ActivateVersion(Guid id, Guid versionId, CancellationToken ct)
    {
        await _workflows.ActivateVersionAsync(versionId, id, ct);
        return Ok();
    }
}
