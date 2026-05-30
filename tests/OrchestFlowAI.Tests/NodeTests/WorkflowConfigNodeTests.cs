using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Nodes.System;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Tests.NodeTests;

public sealed class ReadConfigNodeTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static WorkflowExecutionContext BuildCtx(string key, string? defaultValue,
        WorkflowConfig? stored, out Mock<IWorkflowConfigRepository> repoMock)
    {
        repoMock = new Mock<IWorkflowConfigRepository>();
        repoMock.Setup(r => r.GetAsync(TenantId, key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stored);

        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowConfigRepository>(repoMock.Object);
        var sp = services.BuildServiceProvider();

        var config = new Dictionary<string, object?> { ["key"] = key };
        if (defaultValue != null) config["defaultValue"] = defaultValue;

        return new WorkflowExecutionContext
        {
            ExecutionId    = Guid.NewGuid(),
            TenantId       = TenantId,
            CorrelationId  = Guid.NewGuid().ToString(),
            NodeExecutionId = Guid.NewGuid(),
            NodeInputs     = new Dictionary<string, object?>(),
            NodeConfig     = config,
            WorkflowInputs = new Dictionary<string, object?>(),
            NodeOutputs    = new Dictionary<string, IReadOnlyDictionary<string, object?>>(),
            Services       = sp,
            CurrentNodeId  = "read-config-1",
        };
    }

    [Fact]
    public async Task Execute_KeyExists_ReturnsValue()
    {
        var stored = WorkflowConfig.Create(TenantId, "mykey", "hello", "string");
        var ctx = BuildCtx("mykey", null, stored, out _);
        var result = await new ReadConfigNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["value"].Should().Be("hello");
        result.Outputs["found"].Should().Be(true);
    }

    [Fact]
    public async Task Execute_KeyMissing_ReturnsDefaultValue()
    {
        var ctx = BuildCtx("missing", "fallback", null, out _);
        var result = await new ReadConfigNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["value"].Should().Be("fallback");
        result.Outputs["found"].Should().Be(false);
    }

    [Fact]
    public async Task Execute_KeyMissing_NoDefault_ReturnsEmptyString()
    {
        var ctx = BuildCtx("missing", null, null, out _);
        var result = await new ReadConfigNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["value"].Should().Be("");
        result.Outputs["found"].Should().Be(false);
    }

    [Fact]
    public async Task Execute_NumberType_CoercesToDouble()
    {
        var stored = WorkflowConfig.Create(TenantId, "mynum", "42.5", "number");
        var ctx = BuildCtx("mynum", null, stored, out _);
        var result = await new ReadConfigNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        Convert.ToDouble(result.Outputs["value"]).Should().BeApproximately(42.5, 0.001);
    }

    [Fact]
    public async Task Execute_BooleanType_CoercesToBool()
    {
        var stored = WorkflowConfig.Create(TenantId, "mybool", "true", "boolean");
        var ctx = BuildCtx("mybool", null, stored, out _);
        var result = await new ReadConfigNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["value"].Should().Be(true);
    }

    [Fact]
    public async Task Execute_MissingKeyConfig_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IWorkflowConfigRepository>());
        var sp = services.BuildServiceProvider();

        var ctx = new WorkflowExecutionContext
        {
            ExecutionId = Guid.NewGuid(), TenantId = TenantId,
            CorrelationId = Guid.NewGuid().ToString(), NodeExecutionId = Guid.NewGuid(),
            NodeInputs = new Dictionary<string, object?>(),
            NodeConfig = new Dictionary<string, object?>(),   // no key!
            WorkflowInputs = new Dictionary<string, object?>(),
            NodeOutputs = new Dictionary<string, IReadOnlyDictionary<string, object?>>(),
            Services = sp, CurrentNodeId = "x"
        };

        var act = () => new ReadConfigNode().ExecuteAsync(ctx, CancellationToken.None);
        await act.Should().ThrowAsync<OrchestFlowAI.SDK.Exceptions.NodeExecutionException>();
    }
}

public sealed class WriteConfigNodeTests
{
    private static readonly Guid TenantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static WorkflowExecutionContext BuildCtx(string key, string? inputValue,
        string? configValue, WorkflowConfig? existing, out Mock<IWorkflowConfigRepository> repoMock)
    {
        repoMock = new Mock<IWorkflowConfigRepository>();
        repoMock.Setup(r => r.GetAsync(TenantId, key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
        repoMock.Setup(r => r.CreateAsync(It.IsAny<WorkflowConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((WorkflowConfig c, CancellationToken _) => c);
        repoMock.Setup(r => r.UpdateAsync(It.IsAny<WorkflowConfig>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowConfigRepository>(repoMock.Object);
        var sp = services.BuildServiceProvider();

        var nodeConfig = new Dictionary<string, object?> { ["key"] = key };
        if (configValue != null) nodeConfig["value"] = configValue;

        var nodeInputs = new Dictionary<string, object?>();
        if (inputValue != null) nodeInputs["value"] = inputValue;

        return new WorkflowExecutionContext
        {
            ExecutionId = Guid.NewGuid(), TenantId = TenantId,
            CorrelationId = Guid.NewGuid().ToString(), NodeExecutionId = Guid.NewGuid(),
            NodeInputs = nodeInputs, NodeConfig = nodeConfig,
            WorkflowInputs = new Dictionary<string, object?>(),
            NodeOutputs = new Dictionary<string, IReadOnlyDictionary<string, object?>>(),
            Services = sp, CurrentNodeId = "write-config-1"
        };
    }

    [Fact]
    public async Task Execute_NewKey_CallsCreate()
    {
        var ctx = BuildCtx("newkey", null, "myval", null, out var repoMock);
        var result = await new WriteConfigNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["newValue"].Should().Be("myval");
        result.Outputs["previousValue"].Should().BeNull();
        repoMock.Verify(r => r.CreateAsync(It.IsAny<WorkflowConfig>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ExistingKey_CallsUpdate()
    {
        var existing = WorkflowConfig.Create(TenantId, "existkey", "old", "string");
        var ctx = BuildCtx("existkey", null, "new", existing, out var repoMock);
        var result = await new WriteConfigNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["newValue"].Should().Be("new");
        result.Outputs["previousValue"].Should().Be("old");
        repoMock.Verify(r => r.UpdateAsync(It.IsAny<WorkflowConfig>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_InputValueOverridesConfigValue()
    {
        var ctx = BuildCtx("k", "from-input", "from-config", null, out _);
        var result = await new WriteConfigNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Outputs["newValue"].Should().Be("from-input");
    }

    [Fact]
    public async Task Execute_OutputsContainKey()
    {
        var ctx = BuildCtx("mykey", null, "v", null, out _);
        var result = await new WriteConfigNode().ExecuteAsync(ctx, CancellationToken.None);
        result.Outputs["key"].Should().Be("mykey");
    }
}
