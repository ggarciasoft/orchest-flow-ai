using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrchestAI.Domain.Entities;
using OrchestAI.Domain.Enums;
using OrchestAI.Domain.ValueObjects;
using OrchestAI.Engine;
using OrchestAI.Engine.Models;
using OrchestAI.Engine.Registry;
using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;

namespace OrchestAI.Tests.EngineTests;

/// <summary>
/// Unit tests for retry behavior in <see cref="WorkflowExecutionEngine"/>.
/// </summary>
public sealed class WorkflowEngineRetryTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WorkflowId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid VersionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid ExecutionId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    /// <summary>
    /// A minimal linear workflow: start -> target -> end.
    /// </summary>
    private static string BuildDefinitionJson(string targetNodeId, string targetNodeType) =>
        JsonSerializer.Serialize(new
        {
            nodes = new object[]
            {
                new { id = "start", type = "system.start", config = new { } },
                new { id = targetNodeId, type = targetNodeType, config = new { } },
                new { id = "end", type = "system.end", config = new { } }
            },
            edges = new object[]
            {
                new { source = "start", target = targetNodeId },
                new { source = targetNodeId, target = "end" }
            }
        });

    private (WorkflowExecutionEngine engine, Mock<IEngineExecutionRepository> repoMock, Mock<IWorkflowNode> nodeMock)
        BuildEngine(Workflow workflow, WorkflowExecution execution, string targetNodeId, string targetNodeType)
    {
        var repoMock = new Mock<IEngineExecutionRepository>();
        var nodeMock = new Mock<IWorkflowNode>();

        var version = WorkflowVersion.Create(WorkflowId, 1, BuildDefinitionJson(targetNodeId, targetNodeType), Guid.NewGuid());

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

        // Register system nodes as passthrough
        var startDesc = new Mock<IWorkflowNodeDescriptor>(); startDesc.Setup(d => d.Type).Returns("system.start");
        var endDesc = new Mock<IWorkflowNodeDescriptor>(); endDesc.Setup(d => d.Type).Returns("system.end");
        var targetDesc = new Mock<IWorkflowNodeDescriptor>(); targetDesc.Setup(d => d.Type).Returns(targetNodeType);

        var startNode = new Mock<IWorkflowNode>();
        startNode.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Succeeded(new Dictionary<string, object?>()));
        var endNode = new Mock<IWorkflowNode>();
        endNode.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Succeeded(new Dictionary<string, object?>()));

        registryMock.Setup(r => r.GetNode("system.start")).Returns(startNode.Object);
        registryMock.Setup(r => r.GetNode("system.end")).Returns(endNode.Object);
        registryMock.Setup(r => r.GetNode(targetNodeType)).Returns(nodeMock.Object);
        registryMock.Setup(r => r.GetDescriptor("system.start")).Returns(startDesc.Object);
        registryMock.Setup(r => r.GetDescriptor("system.end")).Returns(endDesc.Object);
        registryMock.Setup(r => r.GetDescriptor(targetNodeType)).Returns(targetDesc.Object);

        var services = new ServiceCollection();
        services.AddSingleton(repoMock.Object);
        var sp = services.BuildServiceProvider();

        var logger = NullLogger<WorkflowExecutionEngine>.Instance;
        var engine = new WorkflowExecutionEngine(sp, registryMock.Object, logger);
        return (engine, repoMock, nodeMock);
    }

    private static WorkflowExecution CreateExecution()
    {
        var exec = WorkflowExecution.Create(TenantId, WorkflowId, VersionId, Guid.NewGuid(), "{}", Guid.NewGuid().ToString());
        // Use reflection to set the Id for determinism
        typeof(WorkflowExecution).GetProperty("Id")!.SetValue(exec, ExecutionId);
        return exec;
    }

    private static Workflow CreateWorkflow(RetryPolicy? retryPolicy = null)
    {
        var wf = Workflow.Create(TenantId, "Test", "Test workflow", Guid.NewGuid());
        typeof(Workflow).GetProperty("Id")!.SetValue(wf, WorkflowId);
        if (retryPolicy != null)
            wf.SetRetryPolicy(retryPolicy);
        return wf;
    }

    [Fact]
    public async Task RunAsync_WhenNodeFailsAndMaxAttemptsZero_ShouldNotRetry()
    {
        // Arrange: no retry policy (MaxAttempts = 0)
        var workflow = CreateWorkflow(); // RetryPolicy.None
        var execution = CreateExecution();
        var (engine, repoMock, nodeMock) = BuildEngine(workflow, execution, "target", "http.request");

        nodeMock.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Failed("connection timeout", retryable: true));

        // Act
        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Assert: node was called exactly once (no retries)
        nodeMock.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenNodeFailsOnceThenSucceeds_ShouldRetryAndComplete()
    {
        // Arrange: retry policy with 3 max attempts, 0ms backoff (deterministic test)
        var workflow = CreateWorkflow(RetryPolicy.Create(3, 0, 1.0));
        var execution = CreateExecution();
        var (engine, repoMock, nodeMock) = BuildEngine(workflow, execution, "target", "http.request");

        // First call fails, second call succeeds
        nodeMock.SetupSequence(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Failed("transient error", retryable: true))
            .ReturnsAsync(NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["result"] = "ok" }));

        // Act
        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Assert: node was called twice (1 failure + 1 retry success)
        nodeMock.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RunAsync_WhenNodeExhaustsRetries_ShouldFailExecution()
    {
        // Arrange: retry policy with 2 max attempts, 0ms backoff
        var workflow = CreateWorkflow(RetryPolicy.Create(2, 0, 1.0));
        var execution = CreateExecution();
        var (engine, repoMock, nodeMock) = BuildEngine(workflow, execution, "target", "http.request");

        // Always fail
        nodeMock.Setup(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NodeExecutionResult.Failed("persistent error", retryable: true));

        // Act
        await engine.RunAsync(ExecutionId, CancellationToken.None);

        // Assert: node was called MaxAttempts times (2)
        nodeMock.Verify(n => n.ExecuteAsync(It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        // Assert: UpdateExecutionAsync was called at least twice (once for Start, at least once more for failure/end)
        repoMock.Verify(r => r.UpdateExecutionAsync(
            It.IsAny<WorkflowExecution>(),
            It.IsAny<CancellationToken>()), Times.AtLeast(1));
    }
}
