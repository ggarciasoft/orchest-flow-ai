using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrchestFlowAI.Api.Controllers;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Events;
using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Tests.ControllerTests;

public sealed class ExternalWebhooksControllerTests
{
    private static readonly Guid ExecutionId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid NodeExecId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static CorrelationToken MakeToken(string kind = "wait", bool used = false, DateTime? expiresAt = null)
    {
        var token = CorrelationToken.Create(ExecutionId, NodeExecId, TenantId, kind, expiresAt.HasValue ? (TimeSpan?)(expiresAt.Value - DateTime.UtcNow) : null);
        if (used) token.MarkUsed();
        return token;
    }

    private static ExternalWebhooksController BuildController(
        Mock<ICorrelationTokenRepository> tokenRepo,
        Mock<IExecutionQueue> queue,
        string body = "{}")
    {
        var controller = new ExternalWebhooksController(tokenRepo.Object, queue.Object, Mock.Of<OrchestFlowAI.Application.Abstractions.IExecutionRepository>(), Mock.Of<OrchestFlowAI.Application.Abstractions.IWorkflowRepository>());
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        httpContext.Request.ContentType = "application/json";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    // ── Resume tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Resume_ValidToken_ResumesExecution()
    {
        var token = MakeToken("wait");
        var tokenRepo = new Mock<ICorrelationTokenRepository>();
        tokenRepo.Setup(r => r.GetByTokenAsync(token.Token, It.IsAny<CancellationToken>())).ReturnsAsync(token);
        tokenRepo.Setup(r => r.UpdateAsync(It.IsAny<CorrelationToken>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var queue = new Mock<IExecutionQueue>();
        queue.Setup(q => q.EnqueueResumeAsync(It.IsAny<ExecutionResumeMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var controller = BuildController(tokenRepo, queue, "{\"key\":\"value\"}");

        var result = await controller.Resume(token.Token, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        queue.Verify(q => q.EnqueueResumeAsync(
            It.Is<ExecutionResumeMessage>(m =>
                m.ExecutionId == ExecutionId &&
                m.NodeExecutionId == NodeExecId &&
                m.ResumeOutputs.ContainsKey("_resumedAt")),
            It.IsAny<CancellationToken>()), Times.Once);
        token.Used.Should().BeTrue();
    }

    [Fact]
    public async Task Resume_UsedToken_Returns410()
    {
        var token = MakeToken("wait", used: true);
        var tokenRepo = new Mock<ICorrelationTokenRepository>();
        tokenRepo.Setup(r => r.GetByTokenAsync(token.Token, It.IsAny<CancellationToken>())).ReturnsAsync(token);

        var controller = BuildController(tokenRepo, new Mock<IExecutionQueue>());

        var result = await controller.Resume(token.Token, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(410);
    }

    [Fact]
    public async Task Resume_ExpiredToken_Returns410()
    {
        var token = CorrelationToken.Create(ExecutionId, NodeExecId, TenantId, "wait", TimeSpan.FromSeconds(-1));
        var tokenRepo = new Mock<ICorrelationTokenRepository>();
        tokenRepo.Setup(r => r.GetByTokenAsync(token.Token, It.IsAny<CancellationToken>())).ReturnsAsync(token);

        var controller = BuildController(tokenRepo, new Mock<IExecutionQueue>());

        var result = await controller.Resume(token.Token, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(410);
    }

    // ── Gate tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Gate_Approved_ResumesWithApprovedTrue()
    {
        var token = MakeToken("gate");
        var tokenRepo = new Mock<ICorrelationTokenRepository>();
        tokenRepo.Setup(r => r.GetByTokenAsync(token.Token, It.IsAny<CancellationToken>())).ReturnsAsync(token);
        tokenRepo.Setup(r => r.UpdateAsync(It.IsAny<CorrelationToken>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var queue = new Mock<IExecutionQueue>();
        queue.Setup(q => q.EnqueueResumeAsync(It.IsAny<ExecutionResumeMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var body = JsonSerializer.Serialize(new { approved = true, reason = "looks good" });
        var controller = BuildController(tokenRepo, queue, body);

        var result = await controller.Gate(token.Token, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        queue.Verify(q => q.EnqueueResumeAsync(
            It.Is<ExecutionResumeMessage>(m =>
                m.ExecutionId == ExecutionId &&
                m.ResumeOutputs.ContainsKey("approved") &&
                Equals(m.ResumeOutputs["approved"], true)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Gate_Rejected_ResumesWithApprovedFalse()
    {
        var token = MakeToken("gate");
        var tokenRepo = new Mock<ICorrelationTokenRepository>();
        tokenRepo.Setup(r => r.GetByTokenAsync(token.Token, It.IsAny<CancellationToken>())).ReturnsAsync(token);
        tokenRepo.Setup(r => r.UpdateAsync(It.IsAny<CorrelationToken>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var queue = new Mock<IExecutionQueue>();
        queue.Setup(q => q.EnqueueResumeAsync(It.IsAny<ExecutionResumeMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var body = JsonSerializer.Serialize(new { approved = false, reason = "not ready" });
        var controller = BuildController(tokenRepo, queue, body);

        var result = await controller.Gate(token.Token, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        queue.Verify(q => q.EnqueueResumeAsync(
            It.Is<ExecutionResumeMessage>(m =>
                m.ResumeOutputs.ContainsKey("approved") &&
                Equals(m.ResumeOutputs["approved"], false)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
