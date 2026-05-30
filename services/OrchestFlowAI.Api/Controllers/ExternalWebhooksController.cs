using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Events;
using System.Text.Json;

namespace OrchestFlowAI.Api.Controllers;

/// <summary>
/// Handles external resume and gate endpoints for webhook-wait and external-gate nodes.
/// All endpoints are anonymous — external systems call these without auth.
/// </summary>
[ApiController, Route("api/webhooks"), AllowAnonymous]
public sealed class ExternalWebhooksController : ControllerBase
{
    private readonly ICorrelationTokenRepository _tokens;
    private readonly IExecutionQueue _queue;
    private readonly IExecutionRepository _execRepo;
    private readonly IWorkflowRepository _wfRepo;
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ExternalWebhooksController(
        ICorrelationTokenRepository tokens,
        IExecutionQueue queue,
        IExecutionRepository execRepo,
        IWorkflowRepository wfRepo)
    {
        _tokens = tokens;
        _queue = queue;
        _execRepo = execRepo;
        _wfRepo = wfRepo;
    }

    /// <summary>
    /// Resumes a waiting workflow execution with the payload from the external system.
    /// </summary>
    /// <param name="token">The correlation token issued by the WaitForWebhookNode.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("resume/{token}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(410)]
    public async Task<IActionResult> Resume(string token, CancellationToken ct)
    {
        var correlationToken = await _tokens.GetByTokenAsync(token, ct);
        if (correlationToken == null)
            return NotFound(new { error = "Token not found." });

        if (correlationToken.Used || (correlationToken.ExpiresAt.HasValue && correlationToken.ExpiresAt.Value < DateTime.UtcNow))
            return StatusCode(410, new { error = "Token has already been used or has expired." });

        if (!string.Equals(correlationToken.Kind, "wait", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(correlationToken.Kind, "data-checkpoint", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Token kind mismatch. Expected 'wait' or 'data-checkpoint'." });

        using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
        var resumeOutputs = new Dictionary<string, object?>();
        foreach (var prop in doc.RootElement.EnumerateObject())
            resumeOutputs[prop.Name] = prop.Value.Clone();

        // Pre-validate for data-checkpoint nodes before consuming token
        if (string.Equals(correlationToken.Kind, "data-checkpoint", StringComparison.OrdinalIgnoreCase))
        {
            var nodeExec = await _execRepo.GetNodeExecutionAsync(correlationToken.NodeExecutionId, ct);
            if (nodeExec != null)
            {
                var wfExec = await _execRepo.GetAsync(correlationToken.ExecutionId, ct);
                if (wfExec != null)
                {
                    var activeVersion = await _wfRepo.GetActiveVersionAsync(wfExec.WorkflowId, ct);
                    if (activeVersion != null)
                    {
                        var def = System.Text.Json.JsonSerializer.Deserialize<OrchestFlowAI.Engine.Models.WorkflowDefinition>(
                            activeVersion.DefinitionJson,
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        var nodeDef = def?.Nodes.FirstOrDefault(n => n.Id == nodeExec.NodeId);
                        if (nodeDef?.Config.TryGetValue("fields", out var fieldsObj) == true
                            && fieldsObj.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var errors = ValidateFields(fieldsObj.GetString()!, resumeOutputs);
                            if (errors.Count > 0)
                                return BadRequest(new { error = "Validation failed. Token not consumed — fix the payload and retry.", errors });
                        }
                    }
                }
            }
        }

        resumeOutputs["_resumedAt"] = DateTime.UtcNow.ToString("O");

        correlationToken.MarkUsed();
        await _tokens.UpdateAsync(correlationToken, ct);

        await _queue.EnqueueResumeAsync(new ExecutionResumeMessage(
            correlationToken.ExecutionId,
            Guid.Empty,
            correlationToken.NodeExecutionId,
            resumeOutputs), ct);

        return Ok(new { status = "resumed" });
    }

    /// <summary>
    /// Resolves an external gate with an approve/reject decision.
    /// </summary>
    /// <param name="token">The correlation token issued by the ExternalGateNode.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("gate/{token}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(410)]
    public async Task<IActionResult> Gate(string token, CancellationToken ct)
    {
        var correlationToken = await _tokens.GetByTokenAsync(token, ct);
        if (correlationToken == null)
            return NotFound(new { error = "Token not found." });

        if (correlationToken.Used || (correlationToken.ExpiresAt.HasValue && correlationToken.ExpiresAt.Value < DateTime.UtcNow))
            return StatusCode(410, new { error = "Token has already been used or has expired." });

        if (!string.Equals(correlationToken.Kind, "gate", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Token kind mismatch. Expected 'gate'." });

        using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
        var root = doc.RootElement;

        var approved = root.TryGetProperty("approved", out var approvedEl) && approvedEl.GetBoolean();
        var reason = root.TryGetProperty("reason", out var reasonEl) ? reasonEl.GetString() : null;

        var resumeOutputs = new Dictionary<string, object?>
        {
            ["approved"] = (object?)approved,
            ["reason"] = reason,
            ["_resumedAt"] = DateTime.UtcNow.ToString("O"),
            ["_correlationToken"] = token
        };

        // Flatten data.* fields if present
        if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in dataEl.EnumerateObject())
                resumeOutputs[$"data.{prop.Name}"] = prop.Value.Clone();
        }

        correlationToken.MarkUsed();
        await _tokens.UpdateAsync(correlationToken, ct);

        await _queue.EnqueueResumeAsync(new ExecutionResumeMessage(
            correlationToken.ExecutionId,
            Guid.Empty,
            correlationToken.NodeExecutionId,
            resumeOutputs), ct);

        return Ok(new { status = "resumed" });
    }

    private sealed record FieldDef(string Key, string Type = "any", bool Required = false);

    private static List<string> ValidateFields(string fieldsJson, Dictionary<string, object?> payload)
    {
        var errors = new List<string>();
        List<FieldDef> fieldDefs;
        try
        {
            fieldDefs = System.Text.Json.JsonSerializer.Deserialize<List<FieldDef>>(fieldsJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch { return errors; }

        foreach (var field in fieldDefs)
        {
            var hasValue = payload.TryGetValue(field.Key, out var rawValue)
                           && rawValue is not null;

            if (hasValue && rawValue is System.Text.Json.JsonElement je)
                hasValue = !(je.ValueKind == System.Text.Json.JsonValueKind.Null
                          || (je.ValueKind == System.Text.Json.JsonValueKind.String && string.IsNullOrWhiteSpace(je.GetString())));

            if (field.Required && !hasValue)
            {
                errors.Add($"Missing required field: '{field.Key}'");
                continue;
            }
            if (!hasValue) continue;

            var strVal = rawValue is System.Text.Json.JsonElement jse
                ? (jse.ValueKind == System.Text.Json.JsonValueKind.String ? jse.GetString()! : jse.GetRawText())
                : rawValue!.ToString()!;

            var typeError = field.Type.ToLowerInvariant() switch
            {
                "number" when !double.TryParse(strVal, System.Globalization.NumberStyles.Any,
                                               System.Globalization.CultureInfo.InvariantCulture, out _)
                    => $"Field '{field.Key}' must be a number (got '{strVal}')",
                "boolean" when strVal is not ("true" or "false" or "True" or "False" or "1" or "0")
                    => $"Field '{field.Key}' must be a boolean (got '{strVal}')",
                _ => null
            };
            if (typeError != null) errors.Add(typeError);
        }
        return errors;
    }
}
