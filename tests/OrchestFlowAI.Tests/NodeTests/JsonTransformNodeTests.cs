using System.Text.Json;
using FluentAssertions;
using OrchestFlowAI.Nodes.Data;
using OrchestFlowAI.SDK.Models;
using OrchestFlowAI.SDK.Testing;

namespace OrchestFlowAI.Tests.NodeTests;

/// <summary>Unit tests for <see cref="JsonTransformNode"/>.</summary>
public sealed class JsonTransformNodeTests
{
    private readonly JsonTransformNode _node = new();

    [Fact]
    public async Task Execute_SimpleFlatMapping_ShouldMapFields()
    {
        var input = JsonSerializer.Serialize(new { userId = "123", name = "Alice" });
        var mapping = JsonSerializer.Serialize(new { id = "userId", username = "name" });

        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["json"] = input })
            .WithConfig(new() { ["mapping"] = mapping })
            .Build();

        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs.Should().ContainKey("transformedJson");
    }

    [Fact]
    public async Task Execute_MissingPath_ShouldReturnNullGracefully()
    {
        var input = JsonSerializer.Serialize(new { name = "Alice" });
        var mapping = JsonSerializer.Serialize(new { missing = "user.id" });

        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["json"] = input })
            .WithConfig(new() { ["mapping"] = mapping })
            .Build();

        var result = await _node.ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["missing"].Should().BeNull();
    }

    [Fact]
    public async Task Execute_MissingJsonInput_ShouldThrow()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["mapping"] = "{}" })
            .Build();

        var act = async () => await _node.ExecuteAsync(ctx, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Type_ShouldBeDataJsonTransform()
    {
        _node.Type.Should().Be("data.json-transform");
    }
}
