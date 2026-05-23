using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OrchestAI.AI.Abstractions;
using OrchestAI.AI.Providers;
using OrchestAI.AI.Routing;
using OrchestAI.Nodes.AI;
using OrchestAI.SDK.Models;
using OrchestAI.SDK.Testing;

namespace OrchestAI.Tests.NodeTests;

/// <summary>Unit tests for <see cref="TranslationNode"/>.</summary>
public sealed class TranslationNodeTests
{
    private static IServiceProvider BuildServices(string inputText, string translatedText)
    {
        var fake = new FakeLLMProvider(new Dictionary<string, string> { [inputText] = translatedText });
        var router = new LLMProviderRouter(new[] { (ILLMProvider)fake }, "fake", "fake-model");
        return new ServiceCollection().AddSingleton(router).BuildServiceProvider();
    }

    [Fact]
    public async Task Execute_ShouldOutputTranslatedTextAndLanguage()
    {
        var services = BuildServices("Hello world", "Hola mundo");
        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["text"] = "Hello world" })
            .WithConfig(new() { ["targetLanguage"] = "Spanish" })
            .WithServices(services)
            .Build();

        var result = await new TranslationNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["translatedText"].Should().Be("Hola mundo");
        result.Outputs["targetLanguage"].Should().Be("Spanish");
    }

    [Fact]
    public async Task Execute_MissingText_ShouldThrow()
    {
        var services = BuildServices("x", "y");
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["targetLanguage"] = "French" })
            .WithServices(services)
            .Build();

        var act = async () => await new TranslationNode().ExecuteAsync(ctx, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Type_ShouldBeAiTranslate()
    {
        new TranslationNode().Type.Should().Be("ai.translate");
    }
}
