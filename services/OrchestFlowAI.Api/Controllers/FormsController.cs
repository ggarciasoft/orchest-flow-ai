using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Events;
using OrchestFlowAI.Contracts.Requests;
using OrchestFlowAI.Contracts.Responses;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Api.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

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
    private readonly IExecutionRepository _executions;
    private readonly IApprovalRepository _approvals;

    public FormsController(IFormRepository forms, IExecutionQueue queue, FormNodeRegistrar registrar, IExecutionRepository executions, IApprovalRepository approvals)
    { _forms = forms; _queue = queue; _registrar = registrar; _executions = executions; _approvals = approvals; }

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

        // Create version 1 and activate it
        var version = FormVersion.Create(form.Id, 1, fieldsJson, UserId);
        version.Activate();
        await _forms.CreateVersionAsync(version, ct);

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

        // Create a new version and activate it
        var nextNum = await _forms.GetNextVersionNumberAsync(id, ct);
        var version = FormVersion.Create(id, nextNum, req.Fields.GetRawText(), UserId);
        version.Activate();
        await _forms.CreateVersionAsync(version, ct);
        // Deactivate previous versions
        await _forms.ActivateVersionAsync(version.Id, id, ct);

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
    // ──────────────────────────────────────────
    // GET /api/forms/{id}/versions
    // ──────────────────────────────────────────

    /// <summary>Lists all versions of a form ordered newest first.</summary>
    [HttpGet("{id:guid}/versions"), Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult> ListVersions(Guid id, CancellationToken ct)
    {
        var form = await _forms.GetAsync(id, TenantId, ct);
        if (form == null) return NotFound();
        var versions = await _forms.ListVersionsAsync(id, ct);
        return Ok(versions.Select(v => new
        {
            id = v.Id,
            versionNumber = v.VersionNumber,
            isActive = v.IsActive,
            createdBy = v.CreatedBy,
            createdAt = v.CreatedAt,
            fieldsJson = v.FieldsJson,
        }));
    }

    // ──────────────────────────────────────────
    // POST /api/forms/{id}/versions/{versionId}/activate
    // ──────────────────────────────────────────

    /// <summary>Activates a specific form version, deactivating all others. The form's FieldsJson is updated to match.</summary>
    [HttpPost("{id:guid}/versions/{versionId:guid}/activate"), Authorize(Policy = "EditorOrAbove")]
    public async Task<IActionResult> ActivateVersion(Guid id, Guid versionId, CancellationToken ct)
    {
        var form = await _forms.GetAsync(id, TenantId, ct);
        if (form == null) return NotFound();
        var version = await _forms.GetVersionAsync(versionId, ct);
        if (version == null || version.FormId != id) return NotFound();

        await _forms.ActivateVersionAsync(versionId, id, ct);

        // Sync the form's live FieldsJson to the activated version
        form.Update(form.Name, form.Slug, form.Description, version.FieldsJson);
        await _forms.UpdateAsync(form, ct);
        await _registrar.RefreshAsync(TenantId, ct);
        return NoContent();
    }

    // ──────────────────────────────────────────
    // GET /api/forms/{id}/fill  (public)
    // ──────────────────────────────────────────

    [HttpGet("{id:guid}/fill"), AllowAnonymous]
    public async Task<ActionResult> Fill(Guid id, [FromQuery] Guid? executionId, [FromQuery] string? nodeExecutionId, CancellationToken ct)
    {
        var forms = await _forms.ListAllAsync(ct);
        var form = forms.FirstOrDefault(f => f.Id == id);
        if (form == null) return NotFound();

        var fields = JsonSerializer.Deserialize<List<FormFieldDefinition>>(form.FieldsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                     ?? new List<FormFieldDefinition>();

        // Resolve optionsFrom: substitute dynamic options from execution node outputs
        if (executionId.HasValue && fields.Any(f => !string.IsNullOrEmpty(f.OptionsFrom)))
        {
            var nodeExecs = await _executions.GetNodeExecutionsAsync(executionId.Value, ct);
            // Build flat map of all succeeded-node output keys -> raw JSON value
            var outputMap = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var ne in nodeExecs.Where(n => n.Status == Domain.Enums.NodeExecutionStatus.Succeeded && n.OutputJson != null))
            {
                try
                {
                    var doc = JsonDocument.Parse(ne.OutputJson!);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                        outputMap.TryAdd(prop.Name, prop.Value.GetRawText());
                }
                catch { /* skip malformed */ }
            }

            fields = fields.Select(f =>
            {
                if (string.IsNullOrEmpty(f.OptionsFrom) || !outputMap.TryGetValue(f.OptionsFrom, out var raw) || raw == null)
                    return f;
                try
                {
                    var doc = JsonDocument.Parse(raw);
                    string[] resolved;
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        resolved = doc.RootElement.EnumerateArray()
                            .Select(el => el.ValueKind == JsonValueKind.String
                                ? el.GetString() ?? el.GetRawText()
                                : el.GetRawText())
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToArray();
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.String)
                    {
                        var inner = JsonDocument.Parse(doc.RootElement.GetString()!);
                        resolved = inner.RootElement.ValueKind == JsonValueKind.Array
                            ? inner.RootElement.EnumerateArray().Select(el => el.GetString() ?? el.GetRawText()).ToArray()
                            : new[] { doc.RootElement.GetString()! };
                    }
                    else { return f; }
                    return f with { Options = resolved, OptionsFrom = null };
                }
                catch { return f; }
            }).ToList();
        }

        return Ok(new
        {
            id = form.Id,
            tenantId = form.TenantId,
            name = form.Name,
            slug = form.Slug,
            description = form.Description,
            fields,
            createdAt = form.CreatedAt,
            updatedAt = form.UpdatedAt,
        });
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

        // Validate regex rules on submitted values
        var fields2 = JsonSerializer.Deserialize<List<FormFieldDefinition>>(form.FieldsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                      ?? new List<FormFieldDefinition>();
        var regexErrors = new List<object>();
        foreach (var field in fields2.Where(f => !string.IsNullOrEmpty(f.ValidationRegex)))
        {
            if (!valuesDict.TryGetValue(field.Key, out var rawVal)) continue;
            var strVal = rawVal as string;
            if (string.IsNullOrEmpty(strVal)) continue; // skip empty unless required (handled above)
            try
            {
                if (!Regex.IsMatch(strVal, field.ValidationRegex!))
                    regexErrors.Add(new { field = field.Key, message = field.ValidationMessage ?? "Invalid format" });
            }
            catch { /* ignore malformed regex */ }
        }
        if (regexErrors.Count > 0)
            return BadRequest(new { detail = "Validation failed", errors = regexErrors });

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

        // Mark the corresponding ApprovalRequest as approved so it leaves the pending inbox
        var approvalRequest = await _approvals.GetByNodeExecutionIdAsync(nodeExecGuid, ct);
        if (approvalRequest != null && approvalRequest.Status == OrchestFlowAI.Domain.Enums.ApprovalStatus.Pending)
        {
            var submitterId = submittedBy ?? Guid.Empty;
            approvalRequest.Approve(submitterId, "Form submitted");
            await _approvals.UpdateAsync(approvalRequest, ct);
        }

        return NoContent();
    }

    private static FormResponse ToResponse(Form f)
    {
        var fields = JsonSerializer.Deserialize<JsonElement>(f.FieldsJson.Length > 0 ? f.FieldsJson : "[]");
        return new(f.Id, f.TenantId, f.Name, f.Slug, f.Description, fields, f.CreatedAt, f.UpdatedAt);
    }
}
