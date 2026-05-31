using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
using OrchestFlowAI.Domain.ValueObjects;
using OrchestFlowAI.Engine;
using OrchestFlowAI.Engine.Registry;
using OrchestFlowAI.Infrastructure.Notifications;
using OrchestFlowAI.Nodes.Logic;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Tests.EngineTests;

/// <summary>
/// Tests for the ForEach loop-mode execution path in <see cref="WorkflowExecutionEngine"/>.
/// Loop mode: logic.foreach → body → logic.foreach.end; the engine runs the body once per item.
/// </summary>
public sealed class WorkflowEngineForEachLoopTests
{
    private static readonly Guid TenantId    = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WorkflowId  = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid VersionId   = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid ExecutionId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    // Workflow shape: start → foreach (loopMode) → body → foreach.end → end
    private static string BuildLoopDefinitionJson() =>
        JsonSerializer.Serialize(new
        {
            nodes = new object[]
            {
                new { id = "start",  type = "system.start",        config = new { } },
                new { id = "fe",     type = "logic.foreach",        config = new { loopMode = true, maxItems = 200 } },
                new { id = "body",   type = "test.body",            config = new { } },
                new { id = "fe-end", type = "logic.foreach.end",    config = new { } },
                new { id = "end",    type = "system.end",           config = new { } }
            },
            edges = new object[]
            {
                new { source = "start",  target = "fe"     },
                new { source = "fe",     target = "body"   },
                new { source = "body",   target = "fe-end" },
                new { source = "fe-end", target = "end"    }
            }
        });

    private static string BuildInputJson(int itemCount)
    {
        var items = Enumerable.Range(1, itemCount).Select(i => (object)i).ToList();
        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["inputArray"] = JsonSerializer.Serialize(items)
        });
    }

    private (WorkflowExecutionEngine engine,
             Mock<IEngineExecutionRepository> repoMock,
             Mock<IWorkflowNode> bodyMock,
             WorkflowExecution execution)
        BuildLoopEngine(int itemCount, RetryPolicy? retryPolicy = null)
    {
        var execution = WorkflowExecution.Create(TenantId, WorkflowId, VersionId,
            Guid.NewGuid(), BuildInputJson(itemCount), Guid.NewGuid().ToString());
        typeof(WorkflowExecution).GetProperty("Id")!.SetValue(execution, ExecutionId);

        var workflow = Workflow.Create(TenantId, "Test", "ForEach Loop Test", Guid.NewGuid());
        typeof(Workflow).GetProperty("Id")!.SetValue(workflow, WorkflowId);
        if (retryPolicy != null) workflow.SetRetryPolicy(retryPolicy);

        var version = WorkflowVersion.Create(WorkflowId, 1, BuildLoopDefinitionJson(), Guid.NewGuid());

        var repoMock = new Mock<IEngineExecutionRepository>();
        repoMock.Setup(r => r.GetExecutionAsync(ExecutionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);
        repoMock.Setup(r => r.GetWorkflowVersionAsync(VersionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);
        repoMock.Setup(r => r.GetWorkflowAsync(WorkflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);
        repoMock.Setup(r => r.CreateNodeExecutionAsync(It.IsAny<NodeExecution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeExecution n, CancellationToken _) => n);
        repoMock.Setup(r => r.UpdateNodeExecutionAsync(It.IsAny<NodeExecution>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.UpdateExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var bodyMock = new Mock<IWorkflowNode>();

        var registryMock = new Mock<INodeRegistry>();
        var passNode = new Mock<IWorkflowNode>();
        passNode.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Succeeded(new Dictionary<string, object?>()));

        registryMock.Setup(r => r.GetNode("system.start"))     .Returns(passNode.Object);
        registryMock.Setup(r => r.GetNode("system.end"))       .Returns(passNode.Object);
        registryMock.Setup(r => r.GetNode("logic.foreach"))    .Returns(new ForEachNode());
        registryMock.Setup(r => r.GetNode("logic.foreach.end")).Returns(new ForEachEndNode());
        registryMock.Setup(r => r.GetNode("test.body"))        .Returns(bodyMock.Object);

        var services = new ServiceCollection();
        services.AddSingleton(repoMock.Object);
        var sp = services.BuildServiceProvider();

        var engine = new WorkflowExecutionEngine(
            sp,
            registryMock.Object,
            new StubExecutionNotifier(),
            NullLogger<WorkflowExecutionEngine>.Instance);

        return (engine, repoMock, bodyMock, execution);
    }

    [Fact]
    public async Task RunAsync_ForEachLoopMode_ExecutesBodyOncePerItem()
    {
        // Arrange: 3-item array, body always succeeds
        var (engine, _, bodyMock, execution) = BuildLoopEngine(itemCount: 3);
        bodyMock.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["processed"] = "ok" }));

        // Act
        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Assert: body ran once per item, execution completed
        bodyMock.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        execution.Status.Should().Be(ExecutionStatus.Completed);
    }

    [Fact]
    public async Task RunAsync_ForEachLoopMode_BodyNodeFails_FailsExecution()
    {
        // Arrange: body always fails
        var (engine, _, bodyMock, execution) = BuildLoopEngine(itemCount: 2);
        bodyMock.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Failed("body error"));

        // Act
        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Assert: execution failed; body ran only once (stopped on first item failure)
        execution.Status.Should().Be(ExecutionStatus.Failed);
        bodyMock.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ForEachLoopMode_BodyNodeRetriesAndSucceeds_CompletesExecution()
    {
        // Arrange: first call fails, second succeeds; retry policy allows 3 attempts
        var (engine, _, bodyMock, execution) = BuildLoopEngine(itemCount: 1, retryPolicy: RetryPolicy.Create(3, 0, 1.0));
        bodyMock.SetupSequence(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Failed("transient error"))
            .ReturnsAsync(NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["result"] = "ok" }));

        // Act
        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Assert: body called twice (1 failure + 1 retry success), execution completed
        bodyMock.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        execution.Status.Should().Be(ExecutionStatus.Completed);
    }

    [Fact]
    public async Task RunAsync_ForEachLoopMode_BodyNodeExhaustsRetries_FailsExecution()
    {
        // Arrange: body always fails; retry policy allows 2 attempts
        var (engine, _, bodyMock, execution) = BuildLoopEngine(itemCount: 1, retryPolicy: RetryPolicy.Create(2, 0, 1.0));
        bodyMock.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Failed("persistent error"));

        // Act
        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Assert: body called MaxAttempts times (2), execution failed
        bodyMock.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        execution.Status.Should().Be(ExecutionStatus.Failed);
    }
}
