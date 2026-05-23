using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OrchestAI.AI.Abstractions;
using OrchestAI.AI.Providers;
using OrchestAI.AI.Routing;
using OrchestAI.Nodes.AI;
using OrchestAI.SDK.Models;
using OrchestAI.SDK.Testing;

namespace OrchestAI.Tests.NodeTests;

/// <summary>Unit tests for <see cref="DataExtractorNode"/>.</summary>
public sealed class DataExtractorNodeTests
{
    private static IServiceProvider BuildServices(string responseJson)
    {
        var fake = new FakeLLMProvider(new Dictionary<string, string> { ["Extract"] = responseJson });
        var router = new LLMProviderRouter(new[] { (ILLMProvider)fake }, "fake", "fake-model");
        return new ServiceCollection().AddSingleton(router).BuildServiceProvider();
    }

    [Fact]
    public async Task Execute_ValidJson_ShouldOutputExtractedJson()
    {
        var services = BuildServices("{\"name\":\"Alice\",\"amount\":\"100\"}");
        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["text"] = "Extract Alice amount 100" })
            .WithConfig(new() { ["fields"] = "name,amount" })
            .WithServices(services)
            .Build();

        var result = await new DataExtractorNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs.Should().ContainKey("extractedJson");
    }

    [Fact]
    public async Task Execute_MissingText_ShouldThrow()
    {
        var services = BuildServices("{}");
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["fields"] = "name" })
            .WithServices(services)
            .Build();

        var act = async () => await new DataExtractorNode().ExecuteAsync(ctx, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Type_ShouldBeAiExtract()
    {
        new DataExtractorNode().Type.Should().Be("ai.extract");
    }
}
