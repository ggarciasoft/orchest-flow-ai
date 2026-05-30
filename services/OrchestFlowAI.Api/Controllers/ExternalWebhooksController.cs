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
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ExternalWebhooksController(ICorrelationTokenRepository tokens, IExecutionQueue queue)
    {
        _tokens = tokens;
        _queue = queue;
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

        // Read raw body as a JSON document
        using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
        var resumeOutputs = new Dictionary<string, object?>();
        foreach (var prop in doc.RootElement.EnumerateObject())
            resumeOutputs[prop.Name] = prop.Value.Clone();
        resumeOutputs["_resumedAt"] = DateTime.UtcNow.ToString("O");

        correlationToken.MarkUsed();
        await _tokens.UpdateAsync(correlationToken, ct);

        await _queue.EnqueueResumeAsync(new ExecutionResumeMessage(
            correlationToken.ExecutionId,
            Guid.Empty,  // no ApprovalRequest entity — use empty guid
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
}
