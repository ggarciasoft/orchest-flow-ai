using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using OrchestFlowAI.Api.Hubs;

namespace OrchestFlowAI.Tests.HubTests;

/// <summary>
/// Tests for <see cref="ExecutionHub"/> group management.
/// </summary>
public sealed class ExecutionHubTests
{
    private static ExecutionHub CreateHub(out Mock<IGroupManager> groupsMock, out Mock<HubCallerContext> contextMock)
    {
        groupsMock = new Mock<IGroupManager>();
        groupsMock.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        groupsMock.Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(c => c.ConnectionId).Returns("test-connection-id");

        var hub = new ExecutionHub
        {
            Groups = groupsMock.Object,
            Context = contextMock.Object
        };
        return hub;
    }

    [Fact]
    public async Task JoinExecution_AddsCallerToCorrectGroup()
    {
        // Arrange
        var hub = CreateHub(out var groupsMock, out _);
        var executionId = "exec-123";

        // Act
        await hub.JoinExecution(executionId);

        // Assert
        groupsMock.Verify(
            g => g.AddToGroupAsync("test-connection-id", $"execution:{executionId}", default),
            Times.Once);
    }

    [Fact]
    public async Task LeaveExecution_RemovesCallerFromCorrectGroup()
    {
        // Arrange
        var hub = CreateHub(out var groupsMock, out _);
        var executionId = "exec-456";

        // Act
        await hub.LeaveExecution(executionId);

        // Assert
        groupsMock.Verify(
            g => g.RemoveFromGroupAsync("test-connection-id", $"execution:{executionId}", default),
            Times.Once);
    }

    [Fact]
    public async Task JoinExecution_DifferentExecutionIds_JoinDifferentGroups()
    {
        // Arrange
        var hub = CreateHub(out var groupsMock, out _);

        // Act
        await hub.JoinExecution("exec-1");
        await hub.JoinExecution("exec-2");

        // Assert
        groupsMock.Verify(g => g.AddToGroupAsync("test-connection-id", "execution:exec-1", default), Times.Once);
        groupsMock.Verify(g => g.AddToGroupAsync("test-connection-id", "execution:exec-2", default), Times.Once);
    }
}
