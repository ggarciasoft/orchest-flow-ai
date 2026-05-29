using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
using FluentAssertions;

namespace OrchestFlowAI.Tests.DomainTests;

public class WorkflowExecutionTests
{
    [Fact]
    public void Create_ShouldInitializeProperly()
    {
        var tenantId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        var workflowVersionId = Guid.NewGuid();
        var triggeredBy = Guid.NewGuid();
        var inputJson = "{\"Key\":\"Value\"}";
        var correlationId = "corr-id-123";

        var execution = WorkflowExecution.Create(tenantId, workflowId, workflowVersionId, triggeredBy, inputJson, correlationId);

        execution.TenantId.Should().Be(tenantId);
        execution.WorkflowId.Should().Be(workflowId);
        execution.WorkflowVersionId.Should().Be(workflowVersionId);
        execution.Status.Should().Be(ExecutionStatus.Queued);
        execution.TriggeredBy.Should().Be(triggeredBy);
        execution.InputJson.Should().Be(inputJson);
        execution.CorrelationId.Should().Be(correlationId);
        execution.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Start_ShouldUpdateStatusToRunning()
    {
        var execution = WorkflowExecution.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{}", "corr-id");

        execution.Start();

        execution.Status.Should().Be(ExecutionStatus.Running);
    }

    [Fact]
    public void Complete_ShouldFinalizeExecution()
    {
        var outputJson = "{\"result\":\"success\"}";
        var execution = WorkflowExecution.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{}", "corr-id");

        execution.Complete(outputJson);

        execution.Status.Should().Be(ExecutionStatus.Completed);
        execution.OutputJson.Should().Be(outputJson);
        execution.CompletedAt.Should().NotBeNull();
        execution.CompletedAt.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Fail_ShouldSetStatusToFailed()
    {
        var errorMessage = "An error occurred.";
        var execution = WorkflowExecution.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{}", "corr-id");

        execution.Fail(errorMessage);

        execution.Status.Should().Be(ExecutionStatus.Failed);
        execution.ErrorMessage.Should().Be(errorMessage);
        execution.CompletedAt.Should().NotBeNull();
        execution.CompletedAt.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Cancel_ShouldSetStatusToCancelledAndRecordCompletedAt()
    {
        var execution = WorkflowExecution.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{}", "corr-id");
        execution.Start();

        execution.Cancel();

        execution.Status.Should().Be(ExecutionStatus.Cancelled);
        execution.CompletedAt.Should().NotBeNull();
        execution.CompletedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Cancel_WhenPaused_ShouldSetStatusToCancelled()
    {
        var execution = WorkflowExecution.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{}", "corr-id");
        execution.Start();
        execution.Pause();

        execution.Cancel();

        execution.Status.Should().Be(ExecutionStatus.Cancelled);
    }
}