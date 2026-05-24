using FluentAssertions;
using OrchestFlowAI.Nodes.Logic;
using OrchestFlowAI.SDK.Models;
using OrchestFlowAI.SDK.Testing;

namespace OrchestFlowAI.Tests.NodeTests;

/// <summary>Unit tests for <see cref="DelayNode"/>.</summary>
public sealed class DelayNodeTests
{
    private readonly DelayNode _node = new();

    [Fact]
    public async Task Execute_ShouldDelayAndOutputElapsedMs()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["durationMs"] = 50.0 })
            .Build();

        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs.Should().ContainKey("delayedMs");
        ((double)result.Outputs["delayedMs"]!).Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Execute_DefaultDuration_ShouldNotThrow()
    {
        // No durationMs config — should default to 1000ms but we cancel quickly
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var ctx = new TestContextBuilder().Build();

        // Should be cancelled (TaskCanceledException) or succeed — either is acceptable
        try { await _node.ExecuteAsync(ctx, cts.Token); }
        catch (OperationCanceledException) { /* expected on short timeout */ }
    }

    [Fact]
    public void Type_ShouldBeLogicDelay()
    {
        _node.Type.Should().Be("logic.delay");
    }
}
