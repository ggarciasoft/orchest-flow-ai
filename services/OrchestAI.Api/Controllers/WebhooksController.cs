using Microsoft.AspNetCore.Mvc;
using OrchestAI.Application.Abstractions;
using OrchestAI.Contracts.Events;
using OrchestAI.Domain.Enums;

namespace OrchestAI.Api.Controllers;

/// <summary>
/// Handles inbound webhook requests that trigger workflow executions.
/// This endpoint is intentionally unauthenticated — verification is performed via X-Webhook-Secret header.
/// </summary>
[ApiController, Route("api/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IWorkflowRepository _workflows;
    private readonly IExecutionRepository _executions;
    private readonly IExecutionQueue _queue;

    /// <summary>Initializes the controller with required repository and queue dependencies.</summary>
    public WebhooksController(IWorkflowRepository workflows, IExecutionRepository executions, IExecutionQueue queue)
    {
        _workflows = workflows;
        _executions = executions;
        _queue = queue;
    }

    /// <summary>
    /// Triggers a workflow execution via inbound webhook.
    /// Verifies the <c>X-Webhook-Secret</c> header against the stored secret for the workflow.
    /// </summary>
    /// <param name="workflowId">The id of the webhook-triggered workflow.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="202">Execution enqueued successfully.</response>
    /// <response code="401">Missing or invalid webhook secret.</response>
    /// <response code="404">Workflow not found or not configured for webhook triggering.</response>
    [HttpPost("{workflowId:guid}")]
    [ProducesResponseType(202)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> TriggerWebhook(Guid workflowId, CancellationToken ct)
    {
        // Resolve the workflow without tenant scoping — webhooks are public endpoints
        var workflow = await _workflows.GetByIdAsync(workflowId, ct);
        if (workflow == null || workflow.TriggerType != TriggerType.Webhook)
            return NotFound();

        var incomingSecret = Request.Headers["X-Webhook-Secret"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(incomingSecret) ||
            !string.Equals(incomingSecret, workflow.WebhookSecret, StringComparison.Ordinal))
            return Unauthorized();

        var activeVersion = await _workflows.GetActiveVersionAsync(workflowId, ct);
        if (activeVersion == null)
            return BadRequest(new { error = "Workflow has no active version." });

        var correlationId = Guid.NewGuid().ToString();
        var execution = Domain.Entities.WorkflowExecution.Create(
            workflow.TenantId,
            workflow.Id,
            activeVersion.Id,
            // Webhook executions are system-initiated; use null as actor
            null,
            inputJson: "{}",
            correlationId: correlationId);

        await _executions.CreateAsync(execution, ct);
        await _queue.EnqueueAsync(new ExecutionQueueMessage(execution.Id, workflow.TenantId, correlationId), ct);

        return Accepted(new { executionId = execution.Id });
    }
}
