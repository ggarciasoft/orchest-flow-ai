using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
using FluentAssertions;

namespace OrchestFlowAI.Tests.DomainTests;

public sealed class NodeExecutionTests
{
    [Fact]
    public void Create_ShouldInitializeWithPendingStatus()
    {
        var execId = Guid.NewGuid();
        var node = NodeExecution.Create(execId, "node-1", "logic.condition", 1);

        node.Id.Should().NotBeEmpty();
        node.WorkflowExecutionId.Should().Be(execId);
        node.NodeId.Should().Be("node-1");
        node.NodeType.Should().Be("logic.condition");
        node.Status.Should().Be(NodeExecutionStatus.Pending);
        node.Step.Should().Be(1);
    }

    [Fact]
    public void Start_ShouldSetStatusToRunning()
    {
        var node = NodeExecution.Create(Guid.NewGuid(), "n1", "type", 0);
        node.Start("{\"key\":\"value\"}");

        node.Status.Should().Be(NodeExecutionStatus.Running);
        node.StartedAt.Should().NotBeNull();
        node.InputJson.Should().Be("{\"key\":\"value\"}");
    }

    [Fact]
    public void Succeed_ShouldSetStatusToSucceeded()
    {
        var node = NodeExecution.Create(Guid.NewGuid(), "n1", "type", 0);
        node.Start(null);
        node.Succeed("{\"result\":true}");

        node.Status.Should().Be(NodeExecutionStatus.Succeeded);
        node.OutputJson.Should().Be("{\"result\":true}");
        node.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Fail_ShouldSetStatusToFailedAndIncrementRetry()
    {
        var node = NodeExecution.Create(Guid.NewGuid(), "n1", "type", 0);
        node.Fail("something went wrong");

        node.Status.Should().Be(NodeExecutionStatus.Failed);
        node.ErrorMessage.Should().Be("something went wrong");
        node.RetryCount.Should().Be(1);
    }

    [Fact]
    public void Skip_ShouldSetStatusToSkipped()
    {
        var node = NodeExecution.Create(Guid.NewGuid(), "n1", "type", 0);
        node.Skip();

        node.Status.Should().Be(NodeExecutionStatus.Skipped);
        node.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_ShouldSetStatusToCancelled()
    {
        var node = NodeExecution.Create(Guid.NewGuid(), "n1", "type", 0);
        node.Cancel();

        node.Status.Should().Be(NodeExecutionStatus.Cancelled);
    }

    [Fact]
    public void WaitForApproval_ShouldSetCorrectStatus()
    {
        var node = NodeExecution.Create(Guid.NewGuid(), "n1", "human.approval", 0);
        node.WaitForApproval("{\"_approvalTitle\":\"Review\"}");

        node.Status.Should().Be(NodeExecutionStatus.WaitingForApproval);
        node.OutputJson.Should().Be("{\"_approvalTitle\":\"Review\"}");
    }
}
