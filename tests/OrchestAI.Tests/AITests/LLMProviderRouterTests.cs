using Moq;
using OrchestAI.AI.Abstractions;
using OrchestAI.AI.Routing;
using FluentAssertions;

namespace OrchestAI.Tests.AITests;

public sealed class LLMProviderRouterTests
{
    private ILLMProvider MakeProvider(string id)
    {
        var mock = new Mock<ILLMProvider>();
        mock.Setup(p => p.Id).Returns(id);
        return mock.Object;
    }

    [Fact]
    public void Route_NullModel_ShouldReturnDefaultProvider()
    {
        var openai = MakeProvider("openai");
        var router = new LLMProviderRouter(new[] { openai }, "openai", "gpt-4o");
        var (provider, model) = router.Route(null);
        provider.Id.Should().Be("openai");
        model.Should().Be("gpt-4o");
    }

    [Fact]
    public void Route_DefaultModel_ShouldReturnDefaultProvider()
    {
        var openai = MakeProvider("openai");
        var router = new LLMProviderRouter(new[] { openai }, "openai", "gpt-4o");
        var (provider, model) = router.Route("default");
        provider.Id.Should().Be("openai");
        model.Should().Be("gpt-4o");
    }

    [Fact]
    public void Route_ProviderSlashModel_ShouldRouteCorrectly()
    {
        var openai = MakeProvider("openai");
        var fake = MakeProvider("fake");
        var router = new LLMProviderRouter(new[] { openai, fake }, "openai", "gpt-4o");
        var (provider, model) = router.Route("fake/fake-model");
        provider.Id.Should().Be("fake");
        model.Should().Be("fake-model");
    }

    [Fact]
    public void Route_UnknownProvider_ShouldFallbackToDefault()
    {
        var openai = MakeProvider("openai");
        var router = new LLMProviderRouter(new[] { openai }, "openai", "gpt-4o");
        var (provider, model) = router.Route("custom-model");
        provider.Id.Should().Be("openai");
        model.Should().Be("custom-model");
    }
}
