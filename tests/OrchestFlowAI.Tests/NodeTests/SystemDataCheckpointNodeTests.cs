using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Nodes.System;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Tests.NodeTests;

public sealed class SystemDataCheckpointNodeTests
{
    private static readonly Guid ExecutionId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid NodeExecId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static (SystemDataCheckpointNode node, Mock<ICorrelationTokenRepository> repoMock, WorkflowExecutionContext ctx) BuildSetup()
    {
        var node = new SystemDataCheckpointNode();
        var repoMock = new Mock<ICorrelationTokenRepository>();
        repoMock
            .Setup(r => r.CreateAsync(It.IsAny<CorrelationToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CorrelationToken t, CancellationToken _) => t);

        var services = new ServiceCollection();
        services.AddSingleton<ICorrelationTokenRepository>(repoMock.Object);
        var sp = services.BuildServiceProvider();

        var ctx = new WorkflowExecutionContext
        {
            ExecutionId = ExecutionId,
            TenantId = TenantId,
            CorrelationId = Guid.NewGuid().ToString(),
            NodeExecutionId = NodeExecId,
            NodeInputs = new Dictionary<string, object?>(),
            NodeConfig = new Dictionary<string, object?>(),
            WorkflowInputs = new Dictionary<string, object?>(),
            NodeOutputs = new Dictionary<string, IReadOnlyDictionary<string, object?>>(),
            Services = sp,
            CurrentNodeId = "test-node"
        };

        return (node, repoMock, ctx);
    }

    private static (SystemDataCheckpointNode node, Mock<ICorrelationTokenRepository> repoMock, WorkflowExecutionContext ctx) BuildResumeCtx(string? fieldsJson, Dictionary<string, object?> inputs)
    {
        var node = new SystemDataCheckpointNode();
        var repoMock = new Mock<ICorrelationTokenRepository>();
        repoMock
            .Setup(r => r.CreateAsync(It.IsAny<CorrelationToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CorrelationToken t, CancellationToken _) => t);

        var services = new ServiceCollection();
        services.AddSingleton<ICorrelationTokenRepository>(repoMock.Object);
        var sp = services.BuildServiceProvider();

        var mergedInputs = new Dictionary<string, object?>(inputs)
        {
            ["_resumedAt"] = DateTime.UtcNow.ToString("O")
        };

        var config = new Dictionary<string, object?>();
        if (fieldsJson != null)
            config["fields"] = fieldsJson;

        var ctx = new WorkflowExecutionContext
        {
            ExecutionId = ExecutionId,
            TenantId = TenantId,
            CorrelationId = Guid.NewGuid().ToString(),
            NodeExecutionId = NodeExecId,
            NodeInputs = mergedInputs,
            NodeConfig = config,
            WorkflowInputs = new Dictionary<string, object?>(),
            NodeOutputs = new Dictionary<string, IReadOnlyDictionary<string, object?>>(),
            Services = sp,
            CurrentNodeId = "test-node"
        };

        return (node, repoMock, ctx);
    }

    [Fact]
    public async Task Execute_NoInputs_ReturnsWaitingForApproval()
    {
        var (node, _, ctx) = BuildSetup();

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.WaitingForApproval);
        result.Outputs.Should().ContainKey("_correlationToken");
        result.Outputs.Should().ContainKey("_resumeUrl");
    }

    [Fact]
    public async Task Execute_FirstExecution_CreatesCorrelationToken()
    {
        var (node, repoMock, ctx) = BuildSetup();

        await node.ExecuteAsync(ctx, CancellationToken.None);

        repoMock.Verify(r => r.CreateAsync(
            It.Is<CorrelationToken>(t =>
                t.ExecutionId == ExecutionId &&
                t.NodeExecutionId == NodeExecId &&
                t.Kind == "data-checkpoint" &&
                !t.Used),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ResumedAt_NoFieldsConfig_ReturnsSucceeded()
    {
        var (node, _, ctx) = BuildResumeCtx(null, new Dictionary<string, object?>
        {
            ["name"] = "Jane"
        });

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["name"].Should().Be("Jane");
        result.Outputs.Should().ContainKey("_validationPassed");
        result.Outputs["_validationPassed"].Should().Be(true);
    }

    [Fact]
    public async Task Execute_ResumedAt_MissingRequiredField_ReturnsFailed()
    {
        var fieldsJson = """[{"key":"name","type":"string","required":true}]""";
        var (node, _, ctx) = BuildResumeCtx(fieldsJson, new Dictionary<string, object?>());

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Failed);
        result.ErrorMessage.Should().Contain("name");
    }

    [Fact]
    public async Task Execute_ResumedAt_RequiredFieldPresent_ReturnsSucceeded()
    {
        var fieldsJson = """[{"key":"name","type":"string","required":true}]""";
        var (node, _, ctx) = BuildResumeCtx(fieldsJson, new Dictionary<string, object?>
        {
            ["name"] = "Jane"
        });

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
    }

    [Fact]
    public async Task Execute_ResumedAt_OptionalFieldMissing_ReturnsSucceeded()
    {
        var fieldsJson = """[{"key":"note","type":"string","required":false}]""";
        var (node, _, ctx) = BuildResumeCtx(fieldsJson, new Dictionary<string, object?>());

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
    }

    [Fact]
    public async Task Execute_ResumedAt_InvalidNumber_ReturnsFailed()
    {
        var fieldsJson = """[{"key":"amount","type":"number","required":true}]""";
        var (node, _, ctx) = BuildResumeCtx(fieldsJson, new Dictionary<string, object?>
        {
            ["amount"] = "notanumber"
        });

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Failed);
        result.ErrorMessage.Should().Contain("number");
    }

    [Fact]
    public async Task Execute_ResumedAt_ValidNumber_CoercesToDouble()
    {
        var fieldsJson = """[{"key":"amount","type":"number","required":true}]""";
        var (node, _, ctx) = BuildResumeCtx(fieldsJson, new Dictionary<string, object?>
        {
            ["amount"] = "42.5"
        });

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["amount"].Should().Be(42.5);
    }

    [Fact]
    public async Task Execute_ResumedAt_InvalidBoolean_ReturnsFailed()
    {
        var fieldsJson = """[{"key":"active","type":"boolean","required":true}]""";
        var (node, _, ctx) = BuildResumeCtx(fieldsJson, new Dictionary<string, object?>
        {
            ["active"] = "maybe"
        });

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Failed);
        result.ErrorMessage.Should().Contain("boolean");
    }

    [Fact]
    public async Task Execute_ResumedAt_ValidBoolean_CoercesToBool()
    {
        var fieldsJson = """[{"key":"active","type":"boolean","required":true}]""";
        var (node, _, ctx) = BuildResumeCtx(fieldsJson, new Dictionary<string, object?>
        {
            ["active"] = "true"
        });

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["active"].Should().Be(true);
    }

    [Fact]
    public async Task Execute_ResumedAt_TypeAny_AcceptsAnything()
    {
        var fieldsJson = """[{"key":"data","type":"any","required":true}]""";
        var (node, _, ctx) = BuildResumeCtx(fieldsJson, new Dictionary<string, object?>
        {
            ["data"] = "whatever"
        });

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
    }

    [Fact]
    public async Task Execute_ResumedAt_MultipleErrors_ReportsAll()
    {
        var fieldsJson = """[{"key":"name","type":"string","required":true},{"key":"email","type":"string","required":true}]""";
        var (node, _, ctx) = BuildResumeCtx(fieldsJson, new Dictionary<string, object?>());

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Failed);
        result.ErrorMessage.Should().Contain("name");
        result.ErrorMessage.Should().Contain("email");
    }

    [Fact]
    public async Task Execute_ResumedAt_Success_ValidationOutputsSet()
    {
        var (node, _, ctx) = BuildResumeCtx(null, new Dictionary<string, object?>
        {
            ["x"] = "1"
        });

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs.Should().ContainKey("_validationPassed");
        result.Outputs.Should().ContainKey("_validationErrors");
    }
}
