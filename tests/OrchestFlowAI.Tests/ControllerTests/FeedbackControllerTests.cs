using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrchestFlowAI.Api.Controllers;
using OrchestFlowAI.Infrastructure.Persistence;
using System.Security.Claims;

namespace OrchestFlowAI.Tests.ControllerTests;

/// <summary>
/// Unit tests for <see cref="FeedbackController"/> — verifies POST /api/feedback behavior.
/// </summary>
public sealed class FeedbackControllerTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static OrchestFlowAIDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<OrchestFlowAIDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrchestFlowAIDbContext(options);
    }

    private static FeedbackController BuildController(OrchestFlowAIDbContext db, Guid? tenantId = null, Guid? userId = null)
    {
        var controller = new FeedbackController(db);
        var claims = new List<Claim>
        {
            new("tenant_id", (tenantId ?? TenantId).ToString()),
            new(ClaimTypes.NameIdentifier, (userId ?? UserId).ToString()),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };
        return controller;
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/feedback — happy path
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_ValidMessage_Returns201WithIdAndTimestamp()
    {
        using var db = CreateInMemoryDb();
        var controller = BuildController(db);

        var result = await controller.Submit(new SubmitFeedbackRequest("Great product!"), CancellationToken.None);

        var status = result.Should().BeOfType<ObjectResult>().Subject;
        status.StatusCode.Should().Be(201);
        var response = status.Value.Should().BeOfType<FeedbackResponse>().Subject;
        response.Id.Should().NotBeEmpty();
        response.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Submit_ValidMessageWithRating_PersistsBothFields()
    {
        using var db = CreateInMemoryDb();
        var controller = BuildController(db);

        await controller.Submit(new SubmitFeedbackRequest("Excellent!", 5), CancellationToken.None);

        var saved = db.Feedbacks.Single();
        saved.Message.Should().Be("Excellent!");
        saved.Rating.Should().Be(5);
        saved.TenantId.Should().Be(TenantId);
        saved.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Submit_PersistsToDatabase()
    {
        using var db = CreateInMemoryDb();
        var controller = BuildController(db);

        await controller.Submit(new SubmitFeedbackRequest("Needs improvement", 3), CancellationToken.None);

        db.Feedbacks.Count().Should().Be(1);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Validation
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_EmptyMessage_ReturnsBadRequest()
    {
        using var db = CreateInMemoryDb();
        var controller = BuildController(db);

        var result = await controller.Submit(new SubmitFeedbackRequest("   "), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Submit_RatingZero_ReturnsBadRequest()
    {
        using var db = CreateInMemoryDb();
        var controller = BuildController(db);

        var result = await controller.Submit(new SubmitFeedbackRequest("Good", 0), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Submit_RatingSix_ReturnsBadRequest()
    {
        using var db = CreateInMemoryDb();
        var controller = BuildController(db);

        var result = await controller.Submit(new SubmitFeedbackRequest("Good", 6), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Submit_RatingNull_Succeeds()
    {
        using var db = CreateInMemoryDb();
        var controller = BuildController(db);

        var result = await controller.Submit(new SubmitFeedbackRequest("No rating provided"), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        db.Feedbacks.Single().Rating.Should().BeNull();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Auth / claim extraction
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_NoTenantIdClaim_ReturnsUnauthorized()
    {
        using var db = CreateInMemoryDb();
        var controller = new FeedbackController(db);
        // No claims set up
        var identity = new ClaimsIdentity([], "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };

        var result = await controller.Submit(new SubmitFeedbackRequest("Hello"), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }
}
