using OrchestAI.Nodes.Human;
using OrchestAI.SDK.Testing;
using OrchestAI.SDK.Models;
using FluentAssertions;

namespace OrchestAI.Tests.NodeTests;

public sealed class HumanApprovalNodeTests
{
    private readonly HumanApprovalNode _node = new();

    [Fact]
    public void Type_ShouldBeHumanApproval()
    {
        _node.Type.Should().Be("human.approval");
    }

    [Fact]
    public async Task Execute_ShouldReturnWaitingForApproval()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["title"] = "Review Contract" })
            .WithInputs(new() { ["documentId"] = "doc-1" })
            .Build();

        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.WaitingForApproval);
        result.Outputs["_approvalTitle"].Should().Be("Review Contract");
        result.Outputs["documentId"].Should().Be("doc-1");
    }

    [Fact]
    public async Task Execute_NoTitleConfig_ShouldUseDefault()
    {
        var ctx = new TestContextBuilder().Build();
        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.WaitingForApproval);
        result.Outputs["_approvalTitle"].Should().Be("Approval Required");
    }
}
