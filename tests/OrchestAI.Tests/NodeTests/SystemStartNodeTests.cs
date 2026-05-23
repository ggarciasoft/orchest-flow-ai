using OrchestAI.Nodes.System;
using OrchestAI.SDK.Testing;
using OrchestAI.SDK.Models;
using FluentAssertions;

namespace OrchestAI.Tests.NodeTests;

public sealed class SystemStartNodeTests
{
    private readonly SystemStartNode _node = new();

    [Fact]
    public void Type_ShouldBeSystemStart()
    {
        _node.Type.Should().Be("system.start");
    }

    [Fact]
    public async Task Execute_ShouldPassThroughWorkflowInputs()
    {
        var ctx = new TestContextBuilder()
            .WithWorkflowInputs(new() { ["documentId"] = "doc-1", ["userId"] = "user-1" })
            .Build();

        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["documentId"].Should().Be("doc-1");
        result.Outputs["userId"].Should().Be("user-1");
    }

    [Fact]
    public async Task Execute_EmptyInputs_ShouldReturnEmptyOutputs()
    {
        var ctx = new TestContextBuilder().Build();
        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs.Should().BeEmpty();
    }
}
