using OrchestAI.Nodes.Logic;
using OrchestAI.SDK.Testing;
using OrchestAI.SDK.Models;
using FluentAssertions;

namespace OrchestAI.Tests.NodeTests;

public sealed class ConditionNodeTests
{
    private readonly ConditionNode _node = new();

    [Fact]
    public async Task Execute_TrueExpression_ShouldReturnTrue()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["expression"] = "status == 'approved'" })
            .WithInputs(new() { ["status"] = "approved" })
            .Build();

        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["result"].Should().Be(true);
    }

    [Fact]
    public async Task Execute_FalseExpression_ShouldReturnFalse()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["expression"] = "status == 'approved'" })
            .WithInputs(new() { ["status"] = "pending" })
            .Build();

        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["result"].Should().Be(false);
    }

    [Fact]
    public async Task Execute_MissingExpression_ShouldThrow()
    {
        var ctx = new TestContextBuilder().Build();
        var act = async () => await _node.ExecuteAsync(ctx, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Type_ShouldBeLogicCondition()
    {
        _node.Type.Should().Be("logic.condition");
    }
}
