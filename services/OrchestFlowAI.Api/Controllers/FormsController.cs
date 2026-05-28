using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Events;
using OrchestFlowAI.Contracts.Requests;
using OrchestFlowAI.Contracts.Responses;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Api.Services;
using System.Text.Json;

namespace OrchestFlowAI.Api.Controllers;

/// <summary>
/// Manages custom form definitions and handles form submissions that resume paused workflow executions.
/// All tenant-scoped endpoints require authentication; the fill endpoint is public.
/// </summary>
[ApiController, Route("api/forms"), Authorize]
public sealed class FormsController : ControllerBase
{
    private readonly IFormRepository _forms;
    private readonly IExecutionQueue _queue;
    private readonly FormNodeRegistrar _registrar;

    public FormsController(IFormRepository forms, IExecutionQueue queue, FormNodeRegistrar registrar)
    { _forms = forms; _queue = queue; _registrar = registrar; }

    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());
    private Guid UserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    // ──────────────────────────────────────────
    // GET /api/forms
    // ──────────────────────────────────────────

    /// <summary>Returns all non-deleted forms for the current tenant.</summary>
    [HttpGet, Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult<IReadOnlyList<FormResponse>>> List(CancellationToken ct)
    {
        var forms = await _forms.ListAsync(TenantId, ct);
        return Ok(forms.Select(ToResponse).ToList());
    }

    // ──────────────────────────────────────────
    // POST /api/forms
    // ──────────────────────────────────────────

    /// <summary>Creates a new form for the current tenant.</summary>
    [HttpPost, Authorize(Policy = "EditorOrAbove")]
    public async Task<ActionResult<FormResponse>> Create([FromBody] CreateFormRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");
        if (string.IsNullOrWhiteSpace(req.Slug)) return BadRequest("Slug is required.");

        var fieldsJson = req.Fields.GetRawText();
        var form = Form.Create(TenantId, req.Name.Trim(), req.Slug.Trim(), req.Description, fieldsJson);
        await _forms.CreateAsync(form, ct);
        await _registrar.RefreshAsync(TenantId, ct);
        return StatusCode(201, ToResponse(form));
    }

    // ──────────────────────────────────────────
    // GET /api/forms/{id}
    // ──────────────────────────────────────────

    /// <summary>Returns a form by its ID.</summary>
    [HttpGet("{id:guid}"), Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult<FormResponse>> Get(Guid id, CancellationToken ct)
    {
        var form = await _forms.GetAsync(id, TenantId, ct);
        if (form == null) return NotFound();
        return Ok(ToResponse(form));
    }

    // ──────────────────────────────────────────
    // PUT /api/forms/{id}
    // ──────────────────────────────────────────

    /// <summary>Updates an existing form.</summary>
    [HttpPut("{id:guid}"), Authorize(Policy = "EditorOrAbove")]
    public async Task<ActionResult<FormResponse>> Update(Guid id, [FromBody] UpdateFormRequest req, CancellationToken ct)
    {
        var form = await _forms.GetAsync(id, TenantId, ct);
        if (form == null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");
        if (string.IsNullOrWhiteSpace(req.Slug)) return BadRequest("Slug is required.");

        form.Update(req.Name.Trim(), req.Slug.Trim(), req.Description, req.Fields.GetRawText());
        await _forms.UpdateAsync(form, ct);
        await _registrar.RefreshAsync(TenantId, ct);
        return Ok(ToResponse(form));
    }

    // ──────────────────────────────────────────
    // DELETE /api/forms/{id}
    // ──────────────────────────────────────────

    /// <summary>Soft-deletes a form.</summary>
    [HttpDelete("{id:guid}"), Authorize(Policy = "EditorOrAbove")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var form = await _forms.GetAsync(id, TenantId, ct);
        if (form == null) return NotFound();
        form.Delete();
        await _forms.UpdateAsync(form, ct);
        await _registrar.RefreshAsync(TenantId, ct);
        return NoContent();
    }

    // ──────────────────────────────────────────
    // GET /api/forms/{id}/fill
    // ──────────────────────────────────────────

    /// <summary>
    /// Public endpoint that returns the form schema for the fill page.
    /// Does not require authentication — allows external users to fill the form.
    /// </summary>
    [HttpGet("{id:guid}/fill"), AllowAnonymous]
    public async Task<ActionResult<FormResponse>> Fill(Guid id, [FromQuery] Guid? executionId, [FromQuery] string? nodeExecutionId, CancellationToken ct)
    {
        // Fetch by id without tenant filter for anonymous access
        // Find the form across all tenants (public fill page)
        var forms = await _forms.ListAllAsync(ct);
        var form = forms.FirstOrDefault(f => f.Id == id);
        if (form == null) return NotFound();
        return Ok(ToResponse(form));
    }

    // ──────────────────────────────────────────
    // POST /api/forms/{id}/submit
    // ──────────────────────────────────────────

    /// <summary>
    /// Submits form values, records the submission, and resumes the paused workflow execution.
    /// </summary>
    [HttpPost("{id:guid}/submit"), AllowAnonymous]
    public async Task<IActionResult> Submit(Guid id, [FromBody] SubmitFormRequest req, CancellationToken ct)
    {
        // Load form (public endpoint — no tenant filter)
        var allForms = await _forms.ListAllAsync(ct);
        var form = allForms.FirstOrDefault(f => f.Id == id);
        if (form == null) return NotFound();

        // Parse submitted values to dictionary for resume
        var valuesDict = new Dictionary<string, object?>();
        if (req.Values.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in req.Values.EnumerateObject())
                valuesDict[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                    ? prop.Value.GetString()
                    : prop.Value.GetRawText();
        }

        // Determine submitter (may be anonymous)
        Guid? submittedBy = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(sub, out var uid)) submittedBy = uid;
        }

        // Store the submission record
        var submission = FormSubmission.Create(
            form.Id,
            req.WorkflowExecutionId,
            req.NodeExecutionId,
            form.TenantId,
            submittedBy,
            req.Values.GetRawText());
        await _forms.CreateSubmissionAsync(submission, ct);

        // Find the NodeExecution id to resume — the NodeExecutionId in the request is the nodeId string;
        // the resume signal requires a Guid NodeExecutionId from the NodeExecution table.
        // The resume queue message uses node execution id as a Guid. Parse if possible.
        Guid nodeExecGuid;
        if (!Guid.TryParse(req.NodeExecutionId, out nodeExecGuid))
            return BadRequest("NodeExecutionId must be a valid GUID.");

        // Build resume outputs: field values become NodeInputs on next execution
        var resumeOutputs = new Dictionary<string, object?>(valuesDict)
        {
            ["_formSubmitted"] = true,
            ["_formId"] = form.Id.ToString()
        };

        await _queue.EnqueueResumeAsync(
            new ExecutionResumeMessage(req.WorkflowExecutionId, nodeExecGuid, nodeExecGuid, resumeOutputs),
            ct);

        return NoContent();
    }

    private static FormResponse ToResponse(Form f) =>
        new(f.Id, f.TenantId, f.Name, f.Slug, f.Description, f.FieldsJson, f.CreatedAt, f.UpdatedAt);
}
