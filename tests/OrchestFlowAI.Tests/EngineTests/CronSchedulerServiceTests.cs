using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Events;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
using OrchestFlowAI.Worker.Workers;

namespace OrchestFlowAI.Tests.EngineTests;

/// <summary>
/// Unit tests for <see cref="CronSchedulerService"/> — verifies due/not-due scheduling logic
/// with a mocked clock and fake workflow repository.
/// </summary>
public sealed class CronSchedulerServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static Workflow MakeCronWorkflow(string expr = "0 * * * *")
    {
        var w = Workflow.Create(TenantId, "Cron WF", "desc", Guid.NewGuid(),
            TriggerType.Cron, cronExpression: expr);
        return w;
    }

    private static WorkflowVersion MakeVersion(Guid workflowId)
    {
        var v = WorkflowVersion.Create(workflowId, 1, "{}", Guid.NewGuid());
        v.Activate();
        return v;
    }

    /// <summary>
    /// Builds a test service with a mocked scope containing the given repositories.
    /// Returns the service and the resolved scope's mocks.
    /// </summary>
    private static (CronSchedulerService service, Mock<IExecutionQueue> queueMock) BuildService(
        Mock<IWorkflowRepository> workflowRepo,
        Mock<IExecutionRepository> execRepo,
        TimeProvider timeProvider)
    {
        var queueMock = new Mock<IExecutionQueue>();
        queueMock.Setup(q => q.EnqueueAsync(It.IsAny<ExecutionQueueMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        execRepo.Setup(r => r.CreateAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowExecution e, CancellationToken _) => e);

        var services = new ServiceCollection();
        services.AddSingleton(workflowRepo.Object);
        services.AddSingleton(execRepo.Object);
        services.AddSingleton(queueMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var service = new CronSchedulerService(
            scopeFactory,
            NullLogger<CronSchedulerService>.Instance,
            timeProvider);

        return (service, queueMock);
    }

    [Fact]
    public async Task EvaluateCronWorkflows_WhenExpressionIsDue_EnqueuesExecution()
    {
        // "* * * * *" fires every minute — always due
        var workflow = MakeCronWorkflow("* * * * *");
        var version = MakeVersion(workflow.Id);

        var workflowRepo = new Mock<IWorkflowRepository>();
        workflowRepo.Setup(r => r.ListByTriggerTypeAsync(TriggerType.Cron, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { workflow });
        workflowRepo.Setup(r => r.GetActiveVersionAsync(workflow.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        // Fix "now" to a known time where the cron is due
        var now = new DateTime(2026, 5, 24, 12, 0, 30, DateTimeKind.Utc);
        var fakeTime = new FakeTimeProvider(now);

        var (service, queue) = BuildService(workflowRepo, new Mock<IExecutionRepository>(), fakeTime);

        await service.EvaluateCronWorkflowsAsync(CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(It.IsAny<ExecutionQueueMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateCronWorkflows_WhenAlreadyTriggeredThisMinute_DoesNotEnqueueAgain()
    {
        var workflow = MakeCronWorkflow("* * * * *");
        var version = MakeVersion(workflow.Id);

        var workflowRepo = new Mock<IWorkflowRepository>();
        workflowRepo.Setup(r => r.ListByTriggerTypeAsync(TriggerType.Cron, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { workflow });
        workflowRepo.Setup(r => r.GetActiveVersionAsync(workflow.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        var now = new DateTime(2026, 5, 24, 12, 0, 30, DateTimeKind.Utc);
        var fakeTime = new FakeTimeProvider(now);
        var execRepo = new Mock<IExecutionRepository>();

        var (service, queue) = BuildService(workflowRepo, execRepo, fakeTime);

        // First evaluation — should enqueue
        await service.EvaluateCronWorkflowsAsync(CancellationToken.None);
        // Second evaluation within the same minute — should NOT enqueue again
        await service.EvaluateCronWorkflowsAsync(CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(It.IsAny<ExecutionQueueMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateCronWorkflows_WhenNextOccurrenceIsInFuture_DoesNotEnqueue()
    {
        // Run every year on Jan 1 at midnight — definitely not due at any other time
        var workflow = MakeCronWorkflow("0 0 1 1 *");
        var version = MakeVersion(workflow.Id);

        var workflowRepo = new Mock<IWorkflowRepository>();
        workflowRepo.Setup(r => r.ListByTriggerTypeAsync(TriggerType.Cron, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { workflow });
        workflowRepo.Setup(r => r.GetActiveVersionAsync(workflow.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        // Set now to mid-May — cron is not due
        var now = new DateTime(2026, 5, 24, 12, 30, 0, DateTimeKind.Utc);
        var fakeTime = new FakeTimeProvider(now);

        var (service, queue) = BuildService(workflowRepo, new Mock<IExecutionRepository>(), fakeTime);

        await service.EvaluateCronWorkflowsAsync(CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(It.IsAny<ExecutionQueueMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateCronWorkflows_InvalidCronExpression_SkipsWorkflowWithoutThrowing()
    {
        var workflow = MakeCronWorkflow("NOT_A_VALID_CRON");

        var workflowRepo = new Mock<IWorkflowRepository>();
        workflowRepo.Setup(r => r.ListByTriggerTypeAsync(TriggerType.Cron, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { workflow });

        var now = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);
        var fakeTime = new FakeTimeProvider(now);

        var (service, queue) = BuildService(workflowRepo, new Mock<IExecutionRepository>(), fakeTime);

        var act = async () => await service.EvaluateCronWorkflowsAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        queue.Verify(q => q.EnqueueAsync(It.IsAny<ExecutionQueueMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateCronWorkflows_NoActiveVersion_SkipsWorkflow()
    {
        var workflow = MakeCronWorkflow("* * * * *");

        var workflowRepo = new Mock<IWorkflowRepository>();
        workflowRepo.Setup(r => r.ListByTriggerTypeAsync(TriggerType.Cron, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { workflow });
        workflowRepo.Setup(r => r.GetActiveVersionAsync(workflow.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowVersion?)null);

        var now = new DateTime(2026, 5, 24, 12, 0, 30, DateTimeKind.Utc);
        var fakeTime = new FakeTimeProvider(now);

        var (service, queue) = BuildService(workflowRepo, new Mock<IExecutionRepository>(), fakeTime);

        await service.EvaluateCronWorkflowsAsync(CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(It.IsAny<ExecutionQueueMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

/// <summary>
/// Minimal <see cref="TimeProvider"/> implementation for deterministic testing.
/// </summary>
internal sealed class FakeTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FakeTimeProvider(DateTime utcNow)
    {
        _now = new DateTimeOffset(utcNow, TimeSpan.Zero);
    }

    public override DateTimeOffset GetUtcNow() => _now;
}
