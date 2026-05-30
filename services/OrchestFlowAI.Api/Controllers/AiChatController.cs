using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;

namespace OrchestFlowAI.Api.Controllers;

/// <summary>
/// Provides read access to AI chat session history — sessions, messages, and token usage.
/// </summary>
[ApiController, Route("api/ai"), Authorize]
public sealed class AiChatController : ControllerBase
{
    private readonly IAiChatRepository _repo;

    public AiChatController(IAiChatRepository repo) => _repo = repo;

    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());

    /// <summary>Lists AI chat sessions for the current tenant.</summary>
    /// <param name="surface">Optional filter: workflow-assist, form-generator, node-assist</param>
    /// <param name="contextId">Optional filter by context entity id</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Results per page (max 50)</param>
    [HttpGet("sessions")]
    public async Task<IActionResult> ListSessions(
        [FromQuery] string? surface = null,
        [FromQuery] Guid?   contextId = null,
        [FromQuery] int     page = 1,
        [FromQuery] int     pageSize = 20,
        CancellationToken   ct = default)
    {
        pageSize = Math.Min(pageSize, 50);
        var sessions = await _repo.ListSessionsAsync(TenantId, surface, contextId, page, pageSize, ct);
        return Ok(sessions.Select(s => new
        {
            id        = s.Id,
            surface   = s.Surface,
            contextId = s.ContextId,
            createdAt = s.CreatedAt,
            updatedAt = s.UpdatedAt,
        }));
    }

    /// <summary>Returns all messages in a session, ordered by time.</summary>
    [HttpGet("sessions/{sessionId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid sessionId, CancellationToken ct)
    {
        var session = await _repo.GetSessionAsync(sessionId, ct);
        if (session == null || session.TenantId != TenantId)
            return NotFound(new { error = "Session not found." });

        var messages = await _repo.GetMessagesAsync(sessionId, ct);
        return Ok(messages.Select(m => new
        {
            id               = m.Id,
            role             = m.Role,
            contentText      = m.ContentText,
            toolName         = m.ToolName,
            toolInputJson    = m.ToolInputJson,
            toolOutputJson   = m.ToolOutputJson,
            promptTokens     = m.PromptTokens,
            completionTokens = m.CompletionTokens,
            totalTokens      = m.TotalTokens,
            model            = m.Model,
            provider         = m.Provider,
            createdAt        = m.CreatedAt,
        }));
    }

    /// <summary>
    /// Returns token usage summary grouped by surface and provider for the current tenant.
    /// </summary>
    [HttpGet("usage-summary")]
    public async Task<IActionResult> UsageSummary(CancellationToken ct)
    {
        var sessions = await _repo.ListSessionsAsync(TenantId, null, null, 1, 1000, ct);
        var summary = new List<object>();
        int totalTokens = 0;

        foreach (var session in sessions)
        {
            var messages = await _repo.GetMessagesAsync(session.Id, ct);
            foreach (var msg in messages.Where(m => m.Role == "assistant"))
                totalTokens += msg.TotalTokens;
        }

        var bySurface = sessions
            .GroupBy(s => s.Surface)
            .Select(g => new { surface = g.Key, sessionCount = g.Count() });

        return Ok(new
        {
            totalSessions = sessions.Count,
            totalTokens,
            bySurface,
        });
    }
}
