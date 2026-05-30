using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Api.Services;

namespace OrchestFlowAI.Api.Controllers;

[ApiController, Route("api/workflows"), Authorize]
public sealed class AiAssistController : ControllerBase
{
    private readonly WorkflowGenerationService _generationService;
    private readonly ILogger<AiAssistController> _logger;

    public AiAssistController(WorkflowGenerationService generationService, ILogger<AiAssistController> logger)
    {
        _generationService = generationService;
        _logger = logger;
    }

    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());

    /// <summary>
    /// Generates or modifies a workflow definition using AI based on a natural language prompt.
    /// </summary>
    /// <param name="req">The prompt, optional current definition, and optional workflow name.</param>
    /// <response code="200">Generated definition, explanation, and change list.</response>
    /// <response code="400">Prompt is empty.</response>
    /// <response code="500">AI generation failed.</response>
    [HttpPost("ai-assist"), Authorize(Policy = "EditorOrAbove")]
    public async Task<ActionResult> AiAssist([FromBody] AiAssistRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest(new { error = "Prompt is required." });

        try
        {
            var genReq = new WorkflowGenerationRequest(req.Prompt, req.CurrentDefinitionJson, req.WorkflowName);
            var result = await _generationService.GenerateAsync(genReq, TenantId, ct);
            return Ok(new
            {
                definition  = result.Definition,
                explanation = result.Explanation,
                changes     = result.Changes,
                provider    = result.Provider,
                model       = result.Model,
                totalTokens = result.TotalTokens,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI workflow generation failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public sealed record AiAssistRequest(
    string Prompt,
    string? CurrentDefinitionJson = null,
    string? WorkflowName = null
);
