using FluentAssertions;
using OrchestAI.Domain.Entities;

namespace OrchestAI.Tests.DomainTests;

/// <summary>
/// Unit tests for retry-related methods on <see cref="NodeExecution"/>.
/// </summary>
public sealed class NodeExecutionRetryTests
{
    [Fact]
    public void Create_Should_HaveAttemptNumberOne()
    {
        var nodeExec = NodeExecution.Create(Guid.NewGuid(), "node-1", "system.start", 1);

        nodeExec.AttemptNumber.Should().Be(1);
    }

    [Fact]
    public void IncrementAttempt_Should_IncreaseAttemptNumber()
    {
        var nodeExec = NodeExecution.Create(Guid.NewGuid(), "node-1", "system.start", 1);

        nodeExec.IncrementAttempt();

        nodeExec.AttemptNumber.Should().Be(2);
    }

    [Fact]
    public void IncrementAttempt_CalledMultipleTimes_Should_AccumulateCorrectly()
    {
        var nodeExec = NodeExecution.Create(Guid.NewGuid(), "node-1", "system.start", 1);

        nodeExec.IncrementAttempt();
        nodeExec.IncrementAttempt();
        nodeExec.IncrementAttempt();

        nodeExec.AttemptNumber.Should().Be(4);
    }

    [Fact]
    public void IncrementAttempt_Should_AlsoIncrementRetryCount()
    {
        var nodeExec = NodeExecution.Create(Guid.NewGuid(), "node-1", "http.request", 2);

        nodeExec.IncrementAttempt();

        nodeExec.RetryCount.Should().Be(1);
    }

    [Fact]
    public void IncrementAttempt_CalledTwice_Should_SetRetryCountToTwo()
    {
        var nodeExec = NodeExecution.Create(Guid.NewGuid(), "node-1", "http.request", 2);

        nodeExec.IncrementAttempt();
        nodeExec.IncrementAttempt();

        nodeExec.RetryCount.Should().Be(2);
    }
}
