using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Api.Controllers;

[ApiController]
[Route("api/presets")]
[Authorize]
public sealed class NodePresetsController : ControllerBase
{
    private readonly INodePresetRepository _repository;

    public NodePresetsController(INodePresetRepository repository)
    {
        _repository = repository;
    }

    [HttpGet, Authorize(Policy = "ViewerOrAbove")]
    public async Task<IActionResult> ListPresets([FromQuery] Guid tenantId, [FromQuery] string? nodeType, CancellationToken ct)
    {
        var presets = await _repository.ListByNodeTypeAsync(tenantId, nodeType, ct);
        var responses = presets.Select(p => new PresetResponse(p.Id, p.Name, p.NodeType, p.ConfigJson, p.CreatedAt)).ToList();
        return Ok(responses);
    }

    [HttpGet("{id}"), Authorize(Policy = "ViewerOrAbove")]
    public async Task<IActionResult> GetPreset(Guid id, [FromQuery] Guid tenantId, CancellationToken ct)
    {
        var preset = await _repository.GetAsync(id, tenantId, ct);
        if (preset == null) return NotFound();

        return Ok(new PresetResponse(preset.Id, preset.Name, preset.NodeType, preset.ConfigJson, preset.CreatedAt));
    }

    [HttpPost, Authorize(Policy = "EditorOrAbove")]
    public async Task<IActionResult> CreatePreset(CreatePresetRequest request, [FromQuery] Guid tenantId, CancellationToken ct)
    {
        var preset = NodePreset.Create(tenantId, request.Name, request.NodeType, request.ConfigJson);
        var created = await _repository.CreateAsync(preset, ct);
        return CreatedAtAction(nameof(GetPreset), new { id = created.Id, tenantId }, new PresetResponse(created.Id, created.Name, created.NodeType, created.ConfigJson, created.CreatedAt));
    }

    [HttpPut("{id}"), Authorize(Policy = "EditorOrAbove")]
    public async Task<IActionResult> UpdatePreset(Guid id, [FromQuery] Guid tenantId, UpdatePresetRequest request, CancellationToken ct)
    {
        var preset = await _repository.GetAsync(id, tenantId, ct);
        if (preset == null) return NotFound();

        preset.Update(request.Name, request.ConfigJson);
        await _repository.UpdateAsync(preset, ct);

        return Ok(new PresetResponse(preset.Id, preset.Name, preset.NodeType, preset.ConfigJson, preset.CreatedAt));
    }

    [HttpDelete("{id}"), Authorize(Policy = "EditorOrAbove")]
    public async Task<IActionResult> DeletePreset(Guid id, [FromQuery] Guid tenantId, CancellationToken ct)
    {
        await _repository.DeleteAsync(id, tenantId, ct);
        return NoContent();
    }
}

public record CreatePresetRequest(string Name, string NodeType, string ConfigJson);
public record UpdatePresetRequest(string Name, string ConfigJson);
public record PresetResponse(Guid Id, string Name, string NodeType, string ConfigJson, DateTime CreatedAt);