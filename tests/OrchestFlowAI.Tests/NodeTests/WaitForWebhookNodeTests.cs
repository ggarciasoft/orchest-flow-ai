using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Nodes.Integrations;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Tests.NodeTests;

public sealed class WaitForWebhookNodeTests
{
    private static readonly Guid ExecutionId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid NodeExecId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static (WaitForWebhookNode node, Mock<ICorrelationTokenRepository> repoMock, WorkflowExecutionContext ctx) BuildSetup()
    {
        var node = new WaitForWebhookNode();
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

    [Fact]
    public async Task Execute_ReturnsWaitingForApproval_WithToken()
    {
        var (node, _, ctx) = BuildSetup();

        var result = await node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.WaitingForApproval);
        result.Outputs.Should().ContainKey("_correlationToken");
        result.Outputs.Should().ContainKey("_resumeUrl");

        var token = result.Outputs["_correlationToken"]?.ToString();
        token.Should().NotBeNullOrEmpty();
        result.Outputs["_resumeUrl"]?.ToString().Should().Be($"/api/webhooks/resume/{token}");
    }

    [Fact]
    public async Task Execute_TokenStoredInRepository()
    {
        var (node, repoMock, ctx) = BuildSetup();

        await node.ExecuteAsync(ctx, CancellationToken.None);

        repoMock.Verify(r => r.CreateAsync(
            It.Is<CorrelationToken>(t =>
                t.ExecutionId == ExecutionId &&
                t.NodeExecutionId == NodeExecId &&
                t.Kind == "wait" &&
                !t.Used),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
