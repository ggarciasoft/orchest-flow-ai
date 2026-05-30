using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Api.Controllers;

[ApiController, Route("api/config"), Authorize]
public sealed class WorkflowConfigController : ControllerBase
{
    private readonly IWorkflowConfigRepository _repo;
    public WorkflowConfigController(IWorkflowConfigRepository repo) => _repo = repo;

    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var entries = await _repo.ListAsync(TenantId, ct);
        return Ok(entries.Select(ToResponse));
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
    {
        var entry = await _repo.GetAsync(TenantId, key, ct);
        if (entry == null) return NotFound(new { error = $"Config key '{key}' not found." });
        return Ok(ToResponse(entry));
    }

    [HttpPost, Authorize(Policy = "EditorOrAbove")]
    public async Task<IActionResult> Create([FromBody] CreateConfigRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Key))  return BadRequest(new { error = "key is required." });
        if (string.IsNullOrWhiteSpace(req.Value)) return BadRequest(new { error = "value is required." });

        var existing = await _repo.GetAsync(TenantId, req.Key, ct);
        if (existing != null) return Conflict(new { error = $"Config key '{req.Key}' already exists. Use PUT to update." });

        var entry = WorkflowConfig.Create(TenantId, req.Key, req.Value, req.ValueType ?? "string", req.Description);
        await _repo.CreateAsync(entry, ct);
        return StatusCode(201, ToResponse(entry));
    }

    [HttpPut("{key}"), Authorize(Policy = "EditorOrAbove")]
    public async Task<IActionResult> Update(string key, [FromBody] UpdateConfigRequest req, CancellationToken ct)
    {
        var entry = await _repo.GetAsync(TenantId, key, ct);
        if (entry == null) return NotFound(new { error = $"Config key '{key}' not found." });
        entry.Update(req.Value ?? entry.Value, req.Description);
        await _repo.UpdateAsync(entry, ct);
        return Ok(ToResponse(entry));
    }

    [HttpDelete("{key}"), Authorize(Policy = "EditorOrAbove")]
    public async Task<IActionResult> Delete(string key, CancellationToken ct)
    {
        var entry = await _repo.GetAsync(TenantId, key, ct);
        if (entry == null) return NotFound(new { error = $"Config key '{key}' not found." });
        await _repo.DeleteAsync(TenantId, key, ct);
        return NoContent();
    }

    private static object ToResponse(WorkflowConfig c) => new
    {
        key         = c.Key,
        value       = c.Value,
        valueType   = c.ValueType,
        description = c.Description,
        updatedAt   = c.UpdatedAt,
    };
}

public sealed record CreateConfigRequest(string Key, string Value, string? ValueType, string? Description);
public sealed record UpdateConfigRequest(string? Value, string? Description);
