using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using OrchestFlowAI.Api.Hubs;
using OrchestFlowAI.Api.Notifications;

namespace OrchestFlowAI.Tests.NotificationTests;

/// <summary>
/// Tests for <see cref="SignalRExecutionNotifier"/> verifying correct group name and method names.
/// </summary>
public sealed class SignalRExecutionNotifierTests
{
    private readonly Mock<IHubContext<ExecutionHub>> _hubContextMock;
    private readonly Mock<IHubClients> _hubClientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly SignalRExecutionNotifier _sut;

    public SignalRExecutionNotifierTests()
    {
        _clientProxyMock = new Mock<IClientProxy>();
        _clientProxyMock
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _hubClientsMock = new Mock<IHubClients>();
        _hubClientsMock
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns(_clientProxyMock.Object);

        _hubContextMock = new Mock<IHubContext<ExecutionHub>>();
        _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);

        _sut = new SignalRExecutionNotifier(_hubContextMock.Object);
    }

    [Fact]
    public async Task NotifyNodeStarted_SendsToCorrectGroupAndMethod()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        // Act
        await _sut.NotifyNodeStarted(executionId, nodeId, "ai.summarize");

        // Assert
        _hubClientsMock.Verify(c => c.Group($"execution:{executionId}"), Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("NodeStarted", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task NotifyNodeCompleted_SendsToCorrectGroupAndMethod()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        // Act
        await _sut.NotifyNodeCompleted(executionId, nodeId, "ai.summarize");

        // Assert
        _hubClientsMock.Verify(c => c.Group($"execution:{executionId}"), Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("NodeCompleted", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task NotifyNodeFailed_SendsToCorrectGroupAndMethod()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        // Act
        await _sut.NotifyNodeFailed(executionId, nodeId, "ai.summarize", "Some error");

        // Assert
        _hubClientsMock.Verify(c => c.Group($"execution:{executionId}"), Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("NodeFailed", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task NotifyExecutionCompleted_SendsToCorrectGroupAndMethod()
    {
        // Arrange
        var executionId = Guid.NewGuid();

        // Act
        await _sut.NotifyExecutionCompleted(executionId, "Completed");

        // Assert
        _hubClientsMock.Verify(c => c.Group($"execution:{executionId}"), Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("ExecutionCompleted", It.IsAny<object[]>(), default),
            Times.Once);
    }
}
