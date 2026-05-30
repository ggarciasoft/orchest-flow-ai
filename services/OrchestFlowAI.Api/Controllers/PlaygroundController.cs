using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Engine;
using System.Text.Json;

namespace OrchestFlowAI.Api.Controllers;

/// <summary>
/// Provides the playground seed endpoint that sets up a sample "User Onboarding" workflow
/// with three form steps for demo/testing purposes.
/// </summary>
[ApiController, Route("api/playground"), Authorize]
public sealed class PlaygroundController : ControllerBase
{
    private readonly IWorkflowRepository _workflows;
    private readonly IFormRepository _forms;
    private readonly IExecutionRepository _executions;
    private readonly IExecutionQueue _queue;
    private readonly Api.Services.FormNodeRegistrar _registrar;

    private const string PlaygroundWorkflowName = "🧪 Playground: User Onboarding";

    public PlaygroundController(
        IWorkflowRepository workflows,
        IFormRepository forms,
        IExecutionRepository executions,
        IExecutionQueue queue,
        Api.Services.FormNodeRegistrar registrar)
    {
        _workflows = workflows;
        _forms = forms;
        _executions = executions;
        _queue = queue;
        _registrar = registrar;
    }

    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());
    private Guid UserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    /// <summary>
    /// Seeds (or upserts) the playground workflow and forms under the current tenant.
    /// Idempotent — safe to call multiple times.
    /// Returns the workflow id and the ids of the three form nodes.
    /// </summary>
    [HttpPost("seed"), Authorize(Policy = "EditorOrAbove")]
    public async Task<ActionResult> Seed(CancellationToken ct)
    {
        // ── 1. Ensure the three forms exist ──────────────────────────────────────

        var form1 = await EnsureForm("pg-personal-info", "Personal Info",
            "Tell us about yourself",
            new[]
            {
                new { key = "full_name",  label = "Full Name",   type = "text",    required = true,  placeholder = (string?)"Jane Smith",         options = (string[]?)null },
                new { key = "email",      label = "Email",       type = "email",   required = true,  placeholder = (string?)"jane@example.com",   options = (string[]?)null },
                new { key = "birthdate",  label = "Date of Birth", type = "date", required = false, placeholder = (string?)null,                 options = (string[]?)null },
            }, ct);

        var form2 = await EnsureForm("pg-employment", "Employment",
            "Where do you work?",
            new[]
            {
                new { key = "company",    label = "Company",     type = "text",    required = true,  placeholder = (string?)"Acme Corp",           options = (string[]?)null },
                new { key = "role",       label = "Job Title",   type = "text",    required = true,  placeholder = (string?)"Software Engineer",   options = (string[]?)null },
                new { key = "start_date", label = "Start Date",  type = "date",    required = false, placeholder = (string?)null,                 options = (string[]?)null },
            }, ct);

        var form3 = await EnsureForm("pg-preferences", "Preferences",
            "Customize your experience",
            new[]
            {
                new { key = "newsletter", label = "Subscribe to newsletter", type = "boolean", required = false, placeholder = (string?)null, options = (string[]?)null },
                new { key = "timezone",   label = "Timezone",   type = "select", required = false, placeholder = (string?)null, options = new[] { "UTC", "America/New_York", "America/La_Paz", "Europe/London", "Asia/Tokyo" } },
                new { key = "notes",      label = "Notes",      type = "text",   required = false, placeholder = (string?)"Anything else we should know…", options = (string[]?)null },
            }, ct);

        // Refresh form node catalog so the workflow can reference them
        await _registrar.RefreshAsync(TenantId, ct);

        // ── 2. Build the workflow definition JSON ────────────────────────────────

        var definition = new
        {
            id = "playground",
            name = PlaygroundWorkflowName,
            version = 1,
            nodes = new object[]
            {
                new { id = "start",  type = "system.start",       position = new { x = 100,  y = 200 }, config = new { } },
                new { id = "step1",  type = $"form.{form1.Slug}", position = new { x = 300,  y = 200 }, config = new { } },
                new { id = "step2",  type = $"form.{form2.Slug}", position = new { x = 550,  y = 200 }, config = new { } },
                new { id = "step3",  type = $"form.{form3.Slug}", position = new { x = 800,  y = 200 }, config = new { } },
                new { id = "end",    type = "system.end",         position = new { x = 1050, y = 200 }, config = new { } },
            },
            edges = new object[]
            {
                new { source = "start", target = "step1" },
                new { source = "step1", target = "step2" },
                new { source = "step2", target = "step3" },
                new { source = "step3", target = "end"   },
            },
        };

        var defJson = JsonSerializer.Serialize(definition);

        // ── 3. Upsert the workflow ────────────────────────────────────────────────

        var workflows = await _workflows.ListAsync(TenantId, PlaygroundWorkflowName, 1, 1, ct);
        Workflow workflow;
        if (workflows.Count == 0)
        {
            workflow = Workflow.Create(TenantId, PlaygroundWorkflowName, "Multi-step form demo workflow for testing.", UserId,
                OrchestFlowAI.Domain.Enums.TriggerType.Manual, null, null);
            await _workflows.CreateAsync(workflow, ct);
        }
        else
        {
            workflow = workflows[0];
        }

        // Always (re)create a fresh version and activate it so changes are reflected
        var nextVersion = (await _workflows.ListVersionsAsync(workflow.Id, ct)).Count + 1;
        var version = WorkflowVersion.Create(workflow.Id, nextVersion, defJson, UserId);
        version.Activate();
        await _workflows.CreateVersionAsync(version, ct);
        await _workflows.ActivateVersionAsync(version.Id, workflow.Id, ct);

        return Ok(new
        {
            workflowId = workflow.Id,
            forms = new { step1 = form1.Id, step2 = form2.Id, step3 = form3.Id },
            message = "Playground seeded successfully."
        });
    }

    /// <summary>
    /// Seeds (or upserts) the "External Data Intake" playground workflow.
    /// Creates a workflow with 2 data-checkpoints and 2 database-execute nodes.
    /// No forms are required — external systems POST data to the resume URLs.
    /// Returns the workflow id.
    /// </summary>
    [HttpPost("seed-external"), Authorize(Policy = "EditorOrAbove")]
    public async Task<ActionResult> SeedExternal(CancellationToken ct)
    {
        const string WorkflowName = "🧪 Playground: External Data Intake";

        var definition = new
        {
            id      = "playground-external",
            name    = WorkflowName,
            version = 1,
            nodes   = new object[]
            {
                new { id = "start",       type = "system.start",            position = new { x = 100,  y = 200 }, config = new { } },
                new { id = "checkpoint1", type = "system.data-checkpoint",  position = new { x = 300,  y = 200 }, config = new { name = "Customer",  description = "POST customer name and email" } },
                new { id = "db1",         type = "data.db-execute",   position = new { x = 550,  y = 200 }, config = new { connectionString = "Server=localhost;Database=Demo;User Id=demo;Password=demo;", query = "INSERT INTO customers (name, email) VALUES (@name, @email)" } },
                new { id = "checkpoint2", type = "system.data-checkpoint",  position = new { x = 800,  y = 200 }, config = new { name = "Order",     description = "POST order items and amount" } },
                new { id = "db2",         type = "data.db-execute",   position = new { x = 1050, y = 200 }, config = new { connectionString = "Server=localhost;Database=Demo;User Id=demo;Password=demo;", query = "INSERT INTO orders (items, amount) VALUES (@items, @amount)" } },
                new { id = "end",         type = "system.end",              position = new { x = 1300, y = 200 }, config = new { } },
            },
            edges = new object[]
            {
                new { source = "start",       target = "checkpoint1" },
                new { source = "checkpoint1", target = "db1"         },
                new { source = "db1",         target = "checkpoint2" },
                new { source = "checkpoint2", target = "db2"         },
                new { source = "db2",         target = "end"         },
            },
        };

        var defJson = JsonSerializer.Serialize(definition);

        var workflows = await _workflows.ListAsync(TenantId, WorkflowName, 1, 1, ct);
        Workflow workflow;
        if (workflows.Count == 0)
        {
            workflow = Workflow.Create(TenantId, WorkflowName,
                "External data intake demo: two data-checkpoints that wait for external system POSTs.",
                UserId, OrchestFlowAI.Domain.Enums.TriggerType.Manual, null, null);
            await _workflows.CreateAsync(workflow, ct);
        }
        else
        {
            workflow = workflows[0];
        }

        var nextVersion = (await _workflows.ListVersionsAsync(workflow.Id, ct)).Count + 1;
        var version     = WorkflowVersion.Create(workflow.Id, nextVersion, defJson, UserId);
        version.Activate();
        await _workflows.CreateVersionAsync(version, ct);
        await _workflows.ActivateVersionAsync(version.Id, workflow.Id, ct);

        return Ok(new { workflowId = workflow.Id, message = "External playground seeded successfully." });
    }

    // ── helpers ──────────────────────────────────────────────────────────────────

    private async Task<Form> EnsureForm(string slug, string name, string description,
        IEnumerable<dynamic> fieldDefs, CancellationToken ct)
    {
        var existing = await _forms.GetBySlugAsync(slug, TenantId, ct);
        if (existing != null) return existing;

        var fields = fieldDefs.Select(f => new FormFieldDefinition(
            Key: (string)f.key,
            Label: (string)f.label,
            Type: (string)f.type,
            Required: (bool)f.required,
            Placeholder: (string?)f.placeholder,
            Options: (string[]?)f.options
        )).ToList();

        var fieldsJson = JsonSerializer.Serialize(fields);
        var form = Form.Create(TenantId, name, slug, description, fieldsJson);
        await _forms.CreateAsync(form, ct);

        var formVersion = FormVersion.Create(form.Id, 1, fieldsJson, UserId);
        formVersion.Activate();
        await _forms.CreateVersionAsync(formVersion, ct);

        return form;
    }
}
