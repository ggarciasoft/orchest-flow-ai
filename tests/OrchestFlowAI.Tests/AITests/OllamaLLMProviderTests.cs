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

public sealed class OllamaLLMProviderTests
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
    public void Id_ShouldBeOllama()
    {
        var provider = new OllamaLLMProvider(Mock.Of<IHttpClientFactory>(), NullLogger<OllamaLLMProvider>.Instance);
        provider.Id.Should().Be("ollama");
    }

    [Fact]
    public void Models_ShouldContainLocalModels()
    {
        var provider = new OllamaLLMProvider(Mock.Of<IHttpClientFactory>(), NullLogger<OllamaLLMProvider>.Instance);
        provider.Models.Should().Contain("llama3");
        provider.Models.Should().Contain("mistral");
    }

    [Fact]
    public async Task GenerateTextAsync_ShouldReturnResponseField()
    {
        var responseBody = JsonSerializer.Serialize(new
        {
            model = "llama3",
            response = "Hello from Ollama!",
            done = true
        });

        var factory = CreateFactory(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });

        var settingsMock = new Mock<IPlatformSettingsService>();
        settingsMock.Setup(s => s.GetAsync(It.IsAny<Guid>(), "llm.ollama.baseUrl", It.IsAny<CancellationToken>()))
            .ReturnsAsync("http://localhost:11434");

        var provider = new OllamaLLMProvider(factory, NullLogger<OllamaLLMProvider>.Instance, settingsMock.Object);
        var request = new LLMRequest { Prompt = "Hello", Model = "llama3", TenantId = Guid.NewGuid() };

        var response = await provider.GenerateTextAsync(request);

        response.Text.Should().Be("Hello from Ollama!");
    }

    [Fact]
    public async Task GenerateTextAsync_UsesDefaultBaseUrl_WhenNotConfigured()
    {
        // Should not throw even with no settings — uses default URL
        var responseBody = JsonSerializer.Serialize(new { response = "ok", done = true });
        var factory = CreateFactory(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });

        var provider = new OllamaLLMProvider(factory, NullLogger<OllamaLLMProvider>.Instance);
        var request = new LLMRequest { Prompt = "Hi", Model = "llama3" };

        var response = await provider.GenerateTextAsync(request);
        response.Text.Should().Be("ok");
    }

    [Fact]
    public async Task GenerateTextAsync_WhenApiReturnsError_ShouldThrow()
    {
        var factory = CreateFactory(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("model not found", Encoding.UTF8, "text/plain")
        });

        var provider = new OllamaLLMProvider(factory, NullLogger<OllamaLLMProvider>.Instance);
        var request = new LLMRequest { Prompt = "Hello", Model = "llama3" };

        var act = () => provider.GenerateTextAsync(request);
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
