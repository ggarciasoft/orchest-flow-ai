using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.AI.Providers;
using FluentAssertions;

namespace OrchestFlowAI.Tests.AITests;

public sealed class FakeLLMProviderTests
{
    [Fact]
    public void Id_ShouldBeFake()
    {
        var provider = new FakeLLMProvider();
        provider.Id.Should().Be("fake");
    }

    [Fact]
    public async Task GenerateTextAsync_WithNoResponses_ShouldReturnDefault()
    {
        var provider = new FakeLLMProvider();
        var request = new LLMRequest { Prompt = "Hello", Model = "fake-model" };

        var response = await provider.GenerateTextAsync(request);

        response.Text.Should().Be("Fake response");
        response.Usage.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateTextAsync_WithMatchingKeyword_ShouldReturnCustomResponse()
    {
        var provider = new FakeLLMProvider(new Dictionary<string, string>
        {
            ["contract"] = "High risk contract"
        });
        var request = new LLMRequest { Prompt = "Analyze this contract please", Model = "fake-model" };

        var response = await provider.GenerateTextAsync(request);

        response.Text.Should().Be("High risk contract");
    }

    [Fact]
    public async Task GenerateStructuredAsync_ShouldDeserializeJson()
    {
        var provider = new FakeLLMProvider(new Dictionary<string, string>
        {
            ["summary"] = "{\"title\":\"Test\",\"content\":\"Summary here\"}"
        });
        var request = new LLMRequest { Prompt = "Write a summary", Model = "fake-model" };

        var response = await provider.GenerateStructuredAsync<TestOutput>(request, "{}");

        response.Output.Title.Should().Be("Test");
        response.Output.Content.Should().Be("Summary here");
    }

    private sealed class TestOutput
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
