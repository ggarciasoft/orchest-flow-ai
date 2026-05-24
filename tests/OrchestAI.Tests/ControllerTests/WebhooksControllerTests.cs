using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrchestAI.Api.Controllers;
using OrchestAI.Application.Abstractions;
using OrchestAI.Contracts.Events;
using OrchestAI.Domain.Entities;
using OrchestAI.Domain.Enums;

namespace OrchestAI.Tests.ControllerTests;

/// <summary>
/// Unit tests for <see cref="WebhooksController"/> — covers secret verification and execution enqueue logic.
/// </summary>
public sealed class WebhooksControllerTests
{
    private static readonly Guid WorkflowId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private const string ValidSecret = "correct-secret";

    private static Workflow MakeWebhookWorkflow()
    {
        var w = Workflow.Create(TenantId, "WH Workflow", "desc", Guid.NewGuid(),
            TriggerType.Webhook, webhookSecret: ValidSecret);
        return w;
    }

    private static WorkflowVersion MakeVersion(Guid workflowId)
    {
        var v = WorkflowVersion.Create(workflowId, 1, "{}", Guid.NewGuid());
        v.Activate();
        return v;
    }

    private static WebhooksController BuildController(
        Mock<IWorkflowRepository> workflows,
        Mock<IExecutionRepository> executions,
        Mock<IExecutionQueue> queue,
        string? secretHeader)
    {
        var controller = new WebhooksController(workflows.Object, executions.Object, queue.Object);
        var headers = new HeaderDictionary();
        if (secretHeader != null)
            headers["X-Webhook-Secret"] = secretHeader;
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Request = { Headers = { } } }
        };
        // Override request headers via a custom mock HttpContext
        var httpContext = new DefaultHttpContext();
        if (secretHeader != null)
            httpContext.Request.Headers["X-Webhook-Secret"] = secretHeader;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task TriggerWebhook_ValidSecret_Returns202AndEnqueues()
    {
        var workflow = MakeWebhookWorkflow();
        var version = MakeVersion(workflow.Id);

        var workflowRepo = new Mock<IWorkflowRepository>();
        workflowRepo.Setup(r => r.GetByIdAsync(workflow.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workflow);
        workflowRepo.Setup(r => r.GetActiveVersionAsync(workflow.Id, It.IsAny<CancellationToken>())).ReturnsAsync(version);

        var execRepo = new Mock<IExecutionRepository>();
        execRepo.Setup(r => r.CreateAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowExecution e, CancellationToken _) => e);

        var queue = new Mock<IExecutionQueue>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<ExecutionQueueMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = BuildController(workflowRepo, execRepo, queue, ValidSecret);

        var result = await controller.TriggerWebhook(workflow.Id, CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        queue.Verify(q => q.EnqueueAsync(It.IsAny<ExecutionQueueMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TriggerWebhook_WrongSecret_Returns401()
    {
        var workflow = MakeWebhookWorkflow();

        var workflowRepo = new Mock<IWorkflowRepository>();
        workflowRepo.Setup(r => r.GetByIdAsync(workflow.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workflow);

        var controller = BuildController(workflowRepo, new Mock<IExecutionRepository>(), new Mock<IExecutionQueue>(), "wrong-secret");

        var result = await controller.TriggerWebhook(workflow.Id, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task TriggerWebhook_MissingSecretHeader_Returns401()
    {
        var workflow = MakeWebhookWorkflow();

        var workflowRepo = new Mock<IWorkflowRepository>();
        workflowRepo.Setup(r => r.GetByIdAsync(workflow.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workflow);

        var controller = BuildController(workflowRepo, new Mock<IExecutionRepository>(), new Mock<IExecutionQueue>(), secretHeader: null);

        var result = await controller.TriggerWebhook(workflow.Id, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task TriggerWebhook_WorkflowNotFound_Returns404()
    {
        var workflowRepo = new Mock<IWorkflowRepository>();
        workflowRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workflow?)null);

        var controller = BuildController(workflowRepo, new Mock<IExecutionRepository>(), new Mock<IExecutionQueue>(), ValidSecret);

        var result = await controller.TriggerWebhook(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task TriggerWebhook_ManualWorkflow_Returns404()
    {
        var workflow = Workflow.Create(TenantId, "Manual", "desc", Guid.NewGuid());
        var workflowRepo = new Mock<IWorkflowRepository>();
        workflowRepo.Setup(r => r.GetByIdAsync(workflow.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workflow);

        var controller = BuildController(workflowRepo, new Mock<IExecutionRepository>(), new Mock<IExecutionQueue>(), ValidSecret);

        var result = await controller.TriggerWebhook(workflow.Id, CancellationToken.None);

        // Non-webhook workflows should return 404 to avoid workflow discovery via the webhook endpoint
        result.Should().BeOfType<NotFoundResult>();
    }
}
