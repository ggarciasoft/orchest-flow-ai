using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OrchestAI.AI.Abstractions;
using OrchestAI.AI.Providers;
using OrchestAI.AI.Routing;
using OrchestAI.Nodes.AI;
using OrchestAI.SDK.Models;
using OrchestAI.SDK.Testing;

namespace OrchestAI.Tests.NodeTests;

/// <summary>Unit tests for <see cref="TextClassifierNode"/>.</summary>
public sealed class TextClassifierNodeTests
{
    private static IServiceProvider BuildServices(Dictionary<string, string> responses)
    {
        var fake = new FakeLLMProvider(responses);
        var router = new LLMProviderRouter(new[] { (ILLMProvider)fake }, "fake", "fake-model");
        return new ServiceCollection().AddSingleton(router).BuildServiceProvider();
    }

    [Fact]
    public async Task Execute_ExactCategoryMatch_ShouldReturnHighConfidence()
    {
        var services = BuildServices(new() { ["this is a contract"] = "urgent" });
        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["text"] = "this is a contract" })
            .WithConfig(new() { ["categories"] = "urgent,normal,low" })
            .WithServices(services)
            .Build();

        var result = await new TextClassifierNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["category"].Should().Be("urgent");
        result.Outputs["confidence"].Should().Be("high");
    }

    [Fact]
    public async Task Execute_NoExactMatch_ShouldReturnLowConfidence()
    {
        var services = BuildServices(new() { ["hello world"] = "zzz_unknown" });
        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["text"] = "hello world" })
            .WithConfig(new() { ["categories"] = "urgent,normal,low" })
            .WithServices(services)
            .Build();

        var result = await new TextClassifierNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["confidence"].Should().Be("low");
    }

    [Fact]
    public void Type_ShouldBeAiClassify()
    {
        new TextClassifierNode().Type.Should().Be("ai.classify");
    }
}
