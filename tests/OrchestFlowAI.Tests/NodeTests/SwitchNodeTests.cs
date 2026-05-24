using FluentAssertions;
using OrchestFlowAI.Nodes.Logic;
using OrchestFlowAI.SDK.Models;
using OrchestFlowAI.SDK.Testing;

namespace OrchestFlowAI.Tests.NodeTests;

/// <summary>Unit tests for <see cref="SwitchNode"/>.</summary>
public sealed class SwitchNodeTests
{
    private readonly SwitchNode _node = new();

    [Fact]
    public async Task Execute_MatchingCase_ShouldReturnMatchedTrue()
    {
        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["value"] = "approved" })
            .WithConfig(new() { ["cases"] = "approved,rejected,pending" })
            .Build();

        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["matched"].Should().Be(true);
        result.Outputs["matchedCase"].Should().Be("approved");
    }

    [Fact]
    public async Task Execute_NoMatchingCase_ShouldReturnDefault()
    {
        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["value"] = "unknown" })
            .WithConfig(new() { ["cases"] = "approved,rejected" })
            .Build();

        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["matched"].Should().Be(false);
        result.Outputs["matchedCase"].Should().Be("default");
    }

    [Fact]
    public async Task Execute_CaseInsensitive_ShouldMatch()
    {
        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["value"] = "APPROVED" })
            .WithConfig(new() { ["cases"] = "approved,rejected" })
            .Build();

        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Outputs["matched"].Should().Be(true);
    }

    [Fact]
    public void Type_ShouldBeLogicSwitch()
    {
        _node.Type.Should().Be("logic.switch");
    }
}
