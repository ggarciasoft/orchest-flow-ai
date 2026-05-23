using System.Text.Json;
using FluentAssertions;
using OrchestAI.Nodes.Data;
using OrchestAI.SDK.Models;
using OrchestAI.SDK.Testing;

namespace OrchestAI.Tests.NodeTests;

/// <summary>Unit tests for <see cref="SetVariableNode"/>.</summary>
public sealed class SetVariableNodeTests
{
    private readonly SetVariableNode _node = new();

    [Fact]
    public async Task Execute_WithPlaceholders_ShouldResolveFromInputs()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["variables"] = JsonSerializer.Serialize(new { greeting = "Hello {{name}}", total = "{{amount}}" }) })
            .WithInputs(new() { ["name"] = "World", ["amount"] = "42" })
            .Build();

        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["greeting"].Should().Be("Hello World");
        result.Outputs["total"].Should().Be("42");
    }

    [Fact]
    public async Task Execute_WithoutPlaceholders_ShouldReturnValuesAsIs()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["variables"] = JsonSerializer.Serialize(new { message = "static text" }) })
            .WithInputs(new())
            .Build();

        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["message"].Should().Be("static text");
    }

    [Fact]
    public async Task Execute_MissingPlaceholderKey_ShouldLeaveUnresolved()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["variables"] = JsonSerializer.Serialize(new { msg = "Hi {{missing}}" }) })
            .WithInputs(new())
            .Build();

        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        // Unresolved placeholders remain in the output since no matching input key exists
        result.Outputs["msg"].Should().Be("Hi {{missing}}");
    }

    [Fact]
    public void Type_ShouldBeDataSet()
    {
        _node.Type.Should().Be("data.set");
    }
}
