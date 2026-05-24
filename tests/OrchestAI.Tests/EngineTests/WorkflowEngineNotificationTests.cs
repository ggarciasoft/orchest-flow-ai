using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrchestAI.Contracts.Notifications;
using OrchestAI.Domain.Entities;
using OrchestAI.Domain.ValueObjects;
using OrchestAI.Engine;
using OrchestAI.Engine.Models;
using OrchestAI.Engine.Registry;
using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;

namespace OrchestAI.Tests.EngineTests;

/// <summary>
/// Verifies that <see cref="WorkflowExecutionEngine"/> calls <see cref="IExecutionNotifier"/>
/// at the correct lifecycle points during execution.
/// </summary>
public sealed class WorkflowEngineNotificationTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WorkflowId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid VersionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid ExecutionId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private static string BuildLinearDefinition() =>
        JsonSerializer.Serialize(new
        {
            nodes = new object[]
            {
                new { id = "start", type = "system.start", config = new { } },
                new { id = "work", type = "test.node", config = new { } },
                new { id = "end", type = "system.end", config = new { } }
            },
            edges = new object[]
            {
                new { source = "start", target = "work" },
                new { source = "work", target = "end" }
            }
        });

    private (WorkflowExecutionEngine engine, Mock<IExecutionNotifier> notifierMock)
        BuildEngine(Func<NodeExecutionResult> nodeResult)
    {
        var repoMock = new Mock<IEngineExecutionRepository>();
        var version = WorkflowVersion.Create(WorkflowId, 1, BuildLinearDefinition(), Guid.NewGuid());

        var exec = WorkflowExecution.Create(TenantId, WorkflowId, VersionId, Guid.NewGuid(), "{}", Guid.NewGuid().ToString());
        typeof(WorkflowExecution).GetProperty("Id")!.SetValue(exec, ExecutionId);

        repoMock.Setup(r => r.GetExecutionAsync(ExecutionId, It.IsAny<CancellationToken>())).ReturnsAsync(exec);
        repoMock.Setup(r => r.GetWorkflowVersionAsync(VersionId, It.IsAny<CancellationToken>())).ReturnsAsync(version);
        repoMock.Setup(r => r.GetWorkflowAsync(WorkflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Workflow.Create(TenantId, "Test", "desc", Guid.NewGuid()));
        repoMock.Setup(r => r.CreateNodeExecutionAsync(It.IsAny<NodeExecution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeExecution n, CancellationToken _) => n);
        repoMock.Setup(r => r.UpdateNodeExecutionAsync(It.IsAny<NodeExecution>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.UpdateExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var registryMock = new Mock<INodeRegistry>();

        // system.start and system.end pass through
        Mock<IWorkflowNode> MakePassthrough(string type)
        {
            var n = new Mock<IWorkflowNode>();
            n.Setup(x => x.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(NodeExecutionResult.Succeeded(new Dictionary<string, object?>()));
            var d = new Mock<IWorkflowNodeDescriptor>();
            d.Setup(x => x.Type).Returns(type);
            registryMock.Setup(r => r.GetNode(type)).Returns(n.Object);
            registryMock.Setup(r => r.GetDescriptor(type)).Returns(d.Object);
            return n;
        }
        MakePassthrough("system.start");
        MakePassthrough("system.end");

        var testNode = new Mock<IWorkflowNode>();
        testNode.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodeResult());
        var testDesc = new Mock<IWorkflowNodeDescriptor>();
        testDesc.Setup(d => d.Type).Returns("test.node");
        registryMock.Setup(r => r.GetNode("test.node")).Returns(testNode.Object);
        registryMock.Setup(r => r.GetDescriptor("test.node")).Returns(testDesc.Object);

        var services = new ServiceCollection();
        services.AddSingleton(repoMock.Object);
        var sp = services.BuildServiceProvider();

        var notifierMock = new Mock<IExecutionNotifier>();
        notifierMock.Setup(n => n.NotifyNodeStarted(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyNodeCompleted(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyNodeFailed(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyExecutionCompleted(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var engine = new WorkflowExecutionEngine(sp, registryMock.Object, notifierMock.Object, NullLogger<WorkflowExecutionEngine>.Instance);
        return (engine, notifierMock);
    }

    [Fact]
    public async Task RunAsync_OnSuccessfulExecution_NotifiesNodeStartedAndCompleted()
    {
        // Arrange
        var (engine, notifierMock) = BuildEngine(() => NodeExecutionResult.Succeeded(new Dictionary<string, object?>()));

        // Act
        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Assert — test.node lifecycle
        notifierMock.Verify(
            n => n.NotifyNodeStarted(ExecutionId, It.IsAny<Guid>(), "test.node", default),
            Times.Once);
        notifierMock.Verify(
            n => n.NotifyNodeCompleted(ExecutionId, It.IsAny<Guid>(), "test.node", default),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_OnSuccessfulExecution_NotifiesExecutionCompleted()
    {
        // Arrange
        var (engine, notifierMock) = BuildEngine(() => NodeExecutionResult.Succeeded(new Dictionary<string, object?>()));

        // Act
        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Assert
        notifierMock.Verify(
            n => n.NotifyExecutionCompleted(ExecutionId, "Completed", default),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RunAsync_WhenNodeFails_NotifiesNodeFailedAndExecutionCompleted()
    {
        // Arrange
        var (engine, notifierMock) = BuildEngine(() => NodeExecutionResult.Failed("timeout error"));

        // Act
        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Assert
        notifierMock.Verify(
            n => n.NotifyNodeFailed(ExecutionId, It.IsAny<Guid>(), "test.node", "timeout error", default),
            Times.Once);
        notifierMock.Verify(
            n => n.NotifyExecutionCompleted(ExecutionId, "Failed", default),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenNodeFails_DoesNotNotifyNodeCompleted()
    {
        // Arrange
        var (engine, notifierMock) = BuildEngine(() => NodeExecutionResult.Failed("fail"));

        // Act
        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Assert — NotifyNodeCompleted must NOT be called for the failed test.node
        notifierMock.Verify(
            n => n.NotifyNodeCompleted(ExecutionId, It.IsAny<Guid>(), "test.node", default),
            Times.Never);
    }
}
