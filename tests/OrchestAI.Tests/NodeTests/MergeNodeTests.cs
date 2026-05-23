using FluentAssertions;
using OrchestAI.Nodes.Logic;
using OrchestAI.SDK.Models;
using OrchestAI.SDK.Testing;

namespace OrchestAI.Tests.NodeTests;

/// <summary>Unit tests for <see cref="MergeNode"/>.</summary>
public sealed class MergeNodeTests
{
    private readonly MergeNode _node = new();

    [Fact]
    public async Task Execute_ShouldForwardAllInputsAsOutputs()
    {
        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["key1"] = "value1", ["key2"] = 42 })
            .Build();

        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["key1"].Should().Be("value1");
        result.Outputs["key2"].Should().Be(42);
    }

    [Fact]
    public async Task Execute_EmptyInputs_ShouldReturnEmptyOutputs()
    {
        var ctx = new TestContextBuilder().Build();
        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs.Should().BeEmpty();
    }

    [Fact]
    public void Type_ShouldBeLogicMerge()
    {
        _node.Type.Should().Be("logic.merge");
    }
}
