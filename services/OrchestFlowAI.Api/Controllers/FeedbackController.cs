using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Infrastructure.Persistence;
using System.Security.Claims;

namespace OrchestFlowAI.Api.Controllers;

/// <summary>
/// Handles user feedback submissions.
/// </summary>
[ApiController]
[Route("api/feedback")]
[Authorize]
public sealed class FeedbackController : ControllerBase
{
    private readonly OrchestFlowAIDbContext _db;

    /// <summary>
    /// Initializes a new instance of <see cref="FeedbackController"/>.
    /// </summary>
    /// <param name="db">The EF Core database context.</param>
    public FeedbackController(OrchestFlowAIDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Submits user feedback. Stores the message and optional rating against the authenticated tenant.
    /// </summary>
    /// <param name="request">The feedback payload containing message and optional rating.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 Created with the feedback id and timestamp.</returns>
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitFeedbackRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message is required.");

        if (request.Rating is < 1 or > 5)
            return BadRequest("Rating must be between 1 and 5.");

        var tenantIdClaim = User.FindFirstValue("tenant_id");
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
            return Unauthorized();

        Guid? userId = Guid.TryParse(userIdClaim, out var uid) ? uid : null;

        var feedback = Feedback.Create(tenantId, request.Message, userId, request.Rating);

        _db.Feedbacks.Add(feedback);
        await _db.SaveChangesAsync(ct);

        return StatusCode(201, new FeedbackResponse(feedback.Id, feedback.CreatedAt));
    }
}

/// <summary>Request body for submitting feedback.</summary>
/// <param name="Message">The feedback message text (required).</param>
/// <param name="Rating">Optional satisfaction rating, 1 (worst) to 5 (best).</param>
public record SubmitFeedbackRequest(string Message, int? Rating = null);

/// <summary>Response returned after a successful feedback submission.</summary>
/// <param name="Id">The unique identifier of the saved feedback entry.</param>
/// <param name="CreatedAt">UTC timestamp of when the feedback was saved.</param>
public record FeedbackResponse(Guid Id, DateTime CreatedAt);
