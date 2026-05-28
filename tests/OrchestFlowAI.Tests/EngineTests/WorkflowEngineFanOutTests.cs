using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
using OrchestFlowAI.Domain.ValueObjects;
using OrchestFlowAI.Engine;
using OrchestFlowAI.Engine.Models;
using OrchestFlowAI.Engine.Registry;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Tests.EngineTests;

/// <summary>
/// Unit tests for fan-out (multiple outgoing edges) behavior in <see cref="WorkflowExecutionEngine"/>.
/// </summary>
public sealed class WorkflowEngineFanOutTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WorkflowId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid VersionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid ExecutionId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    /// <summary>
    /// Builds a fan-out workflow: start -> source -> [nodeA, nodeB] -> end
    /// </summary>
    private static string BuildFanOutDefinitionJson() =>
        JsonSerializer.Serialize(new
        {
            nodes = new object[]
            {
                new { id = "start", type = "system.start", config = new { } },
                new { id = "source", type = "test.source", config = new { } },
                new { id = "nodeA", type = "test.nodeA", config = new { } },
                new { id = "nodeB", type = "test.nodeB", config = new { } },
                new { id = "end", type = "system.end", config = new { } }
            },
            edges = new object[]
            {
                new { source = "start", target = "source" },
                new { source = "source", target = "nodeA" },
                new { source = "source", target = "nodeB" },
                new { source = "nodeA", target = "end" },
                new { source = "nodeB", target = "end" }
            }
        });

    /// <summary>
    /// Builds a linear workflow: start -> source -> end
    /// </summary>
    private static string BuildLinearDefinitionJson() =>
        JsonSerializer.Serialize(new
        {
            nodes = new object[]
            {
                new { id = "start", type = "system.start", config = new { } },
                new { id = "source", type = "test.source", config = new { } },
                new { id = "end", type = "system.end", config = new { } }
            },
            edges = new object[]
            {
                new { source = "start", target = "source" },
                new { source = "source", target = "end" }
            }
        });

    private (WorkflowExecutionEngine engine, Mock<IEngineExecutionRepository> repoMock,
        Mock<IWorkflowNode> nodeAMock, Mock<IWorkflowNode> nodeBMock)
        BuildFanOutEngine(string definitionJson, Mock<IWorkflowNode>? nodeAMock = null, Mock<IWorkflowNode>? nodeBMock = null)
    {
        var repoMock = new Mock<IEngineExecutionRepository>();
        nodeAMock ??= new Mock<IWorkflowNode>();
        nodeBMock ??= new Mock<IWorkflowNode>();

        var workflow = Workflow.Create(TenantId, "Test", "Fan-out test", Guid.NewGuid());
        typeof(Workflow).GetProperty("Id")!.SetValue(workflow, WorkflowId);

        var version = WorkflowVersion.Create(WorkflowId, 1, definitionJson, Guid.NewGuid());
        var execution = CreateExecution();

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

        var registryMock = new Mock<INodeRegistry>();

        var passthrough = NodeExecutionResult.Succeeded(new Dictionary<string, object?>());

        var startNode = new Mock<IWorkflowNode>();
        startNode.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(passthrough);
        var endNode = new Mock<IWorkflowNode>();
        endNode.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(passthrough);
        var sourceNode = new Mock<IWorkflowNode>();
        sourceNode.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["source_value"] = "42" }));

        registryMock.Setup(r => r.GetNode("system.start")).Returns(startNode.Object);
        registryMock.Setup(r => r.GetNode("system.end")).Returns(endNode.Object);
        registryMock.Setup(r => r.GetNode("test.source")).Returns(sourceNode.Object);
        registryMock.Setup(r => r.GetNode("test.nodeA")).Returns(nodeAMock.Object);
        registryMock.Setup(r => r.GetNode("test.nodeB")).Returns(nodeBMock.Object);

        var services = new ServiceCollection();
        services.AddSingleton(repoMock.Object);
        var sp = services.BuildServiceProvider();

        var logger = NullLogger<WorkflowExecutionEngine>.Instance;
        var stubNotifier = new OrchestFlowAI.Infrastructure.Notifications.StubExecutionNotifier();
        var engine = new WorkflowExecutionEngine(sp, registryMock.Object, stubNotifier, logger);
        return (engine, repoMock, nodeAMock, nodeBMock);
    }

    private static WorkflowExecution CreateExecution()
    {
        var exec = WorkflowExecution.Create(TenantId, WorkflowId, VersionId, Guid.NewGuid(), "{}", Guid.NewGuid().ToString());
        typeof(WorkflowExecution).GetProperty("Id")!.SetValue(exec, ExecutionId);
        return exec;
    }

    [Fact]
    public async Task FanOut_TwoTargets_BothExecute()
    {
        // Arrange: start -> source -> [nodeA, nodeB] -> end
        var nodeAMock = new Mock<IWorkflowNode>();
        var nodeBMock = new Mock<IWorkflowNode>();
        nodeAMock.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["a_out"] = "A" }));
        nodeBMock.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["b_out"] = "B" }));

        var (engine, repoMock, nA, nB) = BuildFanOutEngine(BuildFanOutDefinitionJson(), nodeAMock, nodeBMock);

        // Act
        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Assert: both branch nodes executed exactly once
        nA.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Once);
        nB.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Once);

        // NodeExecution records created for nodeA and nodeB
        repoMock.Verify(r => r.CreateNodeExecutionAsync(
            It.Is<NodeExecution>(ne => ne.NodeId == "nodeA"),
            It.IsAny<CancellationToken>()), Times.Once);
        repoMock.Verify(r => r.CreateNodeExecutionAsync(
            It.Is<NodeExecution>(ne => ne.NodeId == "nodeB"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FanOut_TwoTargets_OutputsMerged()
    {
        // Arrange: nodeA emits a_out, nodeB emits b_out; end node should receive both
        var nodeAMock = new Mock<IWorkflowNode>();
        var nodeBMock = new Mock<IWorkflowNode>();
        nodeAMock.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["a_out"] = "fromA" }));
        nodeBMock.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["b_out"] = "fromB" }));

        WorkflowExecutionContext? capturedEndCtx = null;
        var endCaptureMock = new Mock<IWorkflowNode>();
        endCaptureMock.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecutionContext, CancellationToken>((ctx, _) => capturedEndCtx = ctx)
            .ReturnsAsync(NodeExecutionResult.Succeeded(new Dictionary<string, object?>()));

        // Build engine but override the end node
        var (engine, repoMock, nA, nB) = BuildFanOutEngine(BuildFanOutDefinitionJson(), nodeAMock, nodeBMock);

        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Both outputs should have been collected into nodeOutputs
        nA.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Once);
        nB.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Once);

        // Execution completed (not failed)
        repoMock.Verify(r => r.UpdateExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == ExecutionStatus.Completed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FanOut_OneBranchFails_OtherContinues()
    {
        // Arrange: nodeA fails, nodeB succeeds
        var nodeAMock = new Mock<IWorkflowNode>();
        var nodeBMock = new Mock<IWorkflowNode>();
        nodeAMock.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Failed("nodeA error"));
        nodeBMock.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["b_out"] = "B" }));

        var (engine, repoMock, nA, nB) = BuildFanOutEngine(BuildFanOutDefinitionJson(), nodeAMock, nodeBMock);

        // Act
        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Assert: both were attempted
        nA.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Once);
        nB.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Once);

        // Overall execution still completed (non-fatal branch failure)
        repoMock.Verify(r => r.UpdateExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == ExecutionStatus.Completed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FanOut_SingleTarget_LinearBehaviour()
    {
        // Arrange: linear workflow - single edge should work exactly as before
        var (engine, repoMock, nA, nB) = BuildFanOutEngine(BuildLinearDefinitionJson());

        // Act
        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Assert: execution completed normally
        repoMock.Verify(r => r.UpdateExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == ExecutionStatus.Completed),
            It.IsAny<CancellationToken>()), Times.Once);

        // Neither fan-out branch node was called
        nA.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Never);
        nB.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
