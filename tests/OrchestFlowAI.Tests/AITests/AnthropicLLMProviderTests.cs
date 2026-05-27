using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.AI.Providers;
using OrchestFlowAI.Application.Abstractions;

namespace OrchestFlowAI.Tests.AITests;

public sealed class AnthropicLLMProviderTests
{
    private static IHttpClientFactory CreateFactory(HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var client = new HttpClient(handler.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    [Fact]
    public void Id_ShouldBeAnthropic()
    {
        var provider = new AnthropicLLMProvider(Mock.Of<IHttpClientFactory>(), NullLogger<AnthropicLLMProvider>.Instance);
        provider.Id.Should().Be("anthropic");
    }

    [Fact]
    public void Models_ShouldContainClaudeModels()
    {
        var provider = new AnthropicLLMProvider(Mock.Of<IHttpClientFactory>(), NullLogger<AnthropicLLMProvider>.Instance);
        provider.Models.Should().Contain("claude-3-5-sonnet-20241022");
        provider.Models.Should().Contain("claude-3-haiku-20240307");
    }

    [Fact]
    public async Task GenerateTextAsync_ShouldReturnExtractedText()
    {
        var responseBody = JsonSerializer.Serialize(new
        {
            content = new[] { new { type = "text", text = "Hello from Claude!" } },
            usage = new { input_tokens = 10, output_tokens = 5 }
        });

        var factory = CreateFactory(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });

        var settingsMock = new Mock<IPlatformSettingsService>();
        settingsMock.Setup(s => s.GetAsync(It.IsAny<Guid>(), "llm.anthropic.apiKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-api-key");

        var provider = new AnthropicLLMProvider(factory, NullLogger<AnthropicLLMProvider>.Instance, settingsMock.Object);
        var request = new LLMRequest { Prompt = "Hello", Model = "claude-3-haiku-20240307", TenantId = Guid.NewGuid() };

        var response = await provider.GenerateTextAsync(request);

        response.Text.Should().Be("Hello from Claude!");
        response.Usage.PromptTokens.Should().Be(10);
        response.Usage.CompletionTokens.Should().Be(5);
    }

    [Fact]
    public async Task GenerateTextAsync_WhenApiKeyMissing_ShouldThrow()
    {
        var provider = new AnthropicLLMProvider(Mock.Of<IHttpClientFactory>(), NullLogger<AnthropicLLMProvider>.Instance);
        var request = new LLMRequest { Prompt = "Hello", Model = "claude-3-haiku-20240307" };

        var act = () => provider.GenerateTextAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*API key*");
    }

    [Fact]
    public async Task GenerateTextAsync_WhenApiReturnsError_ShouldThrow()
    {
        var factory = CreateFactory(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"error\":{\"message\":\"invalid api key\"}}", Encoding.UTF8, "application/json")
        });

        var settingsMock = new Mock<IPlatformSettingsService>();
        settingsMock.Setup(s => s.GetAsync(It.IsAny<Guid>(), "llm.anthropic.apiKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync("bad-key");

        var provider = new AnthropicLLMProvider(factory, NullLogger<AnthropicLLMProvider>.Instance, settingsMock.Object);
        var request = new LLMRequest { Prompt = "Hello", TenantId = Guid.NewGuid() };

        var act = () => provider.GenerateTextAsync(request);
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
