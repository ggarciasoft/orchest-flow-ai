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

public sealed class AzureOpenAILLMProviderTests
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

    private static Mock<IPlatformSettingsService> CreateSettingsMock(string endpoint, string apiKey, string deployment)
    {
        var mock = new Mock<IPlatformSettingsService>();
        mock.Setup(s => s.GetAsync(It.IsAny<Guid>(), "llm.azure.endpoint", It.IsAny<CancellationToken>()))
            .ReturnsAsync(endpoint);
        mock.Setup(s => s.GetAsync(It.IsAny<Guid>(), "llm.azure.apiKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiKey);
        mock.Setup(s => s.GetAsync(It.IsAny<Guid>(), "llm.azure.deploymentName", It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployment);
        return mock;
    }

    [Fact]
    public void Id_ShouldBeAzure()
    {
        var provider = new AzureOpenAILLMProvider(Mock.Of<IHttpClientFactory>(), NullLogger<AzureOpenAILLMProvider>.Instance);
        provider.Id.Should().Be("azure");
    }

    [Fact]
    public async Task GenerateTextAsync_ShouldReturnChoicesContent()
    {
        var responseBody = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { role = "assistant", content = "Hello from Azure!" } }
            },
            usage = new { prompt_tokens = 10, completion_tokens = 5, total_tokens = 15 }
        });

        var factory = CreateFactory(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });

        var settingsMock = CreateSettingsMock(
            "https://my-resource.openai.azure.com",
            "test-azure-key",
            "gpt-4o-deployment");

        var provider = new AzureOpenAILLMProvider(factory, NullLogger<AzureOpenAILLMProvider>.Instance, settingsMock.Object);
        var request = new LLMRequest { Prompt = "Hello", Model = "default", TenantId = Guid.NewGuid() };

        var response = await provider.GenerateTextAsync(request);

        response.Text.Should().Be("Hello from Azure!");
        response.Usage.PromptTokens.Should().Be(10);
        response.Usage.CompletionTokens.Should().Be(5);
        response.Usage.TotalTokens.Should().Be(15);
    }

    [Fact]
    public async Task GenerateTextAsync_WhenEndpointMissing_ShouldThrow()
    {
        var settingsMock = new Mock<IPlatformSettingsService>();
        settingsMock.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var provider = new AzureOpenAILLMProvider(Mock.Of<IHttpClientFactory>(), NullLogger<AzureOpenAILLMProvider>.Instance, settingsMock.Object);
        var request = new LLMRequest { Prompt = "Hello", TenantId = Guid.NewGuid() };

        var act = () => provider.GenerateTextAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*endpoint*");
    }

    [Fact]
    public async Task GenerateTextAsync_WhenApiKeyMissing_ShouldThrow()
    {
        var settingsMock = new Mock<IPlatformSettingsService>();
        settingsMock.Setup(s => s.GetAsync(It.IsAny<Guid>(), "llm.azure.endpoint", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://my-resource.openai.azure.com");
        settingsMock.Setup(s => s.GetAsync(It.IsAny<Guid>(), "llm.azure.apiKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        settingsMock.Setup(s => s.GetAsync(It.IsAny<Guid>(), "llm.azure.deploymentName", It.IsAny<CancellationToken>()))
            .ReturnsAsync("gpt-4o");

        var provider = new AzureOpenAILLMProvider(Mock.Of<IHttpClientFactory>(), NullLogger<AzureOpenAILLMProvider>.Instance, settingsMock.Object);
        var request = new LLMRequest { Prompt = "Hello", TenantId = Guid.NewGuid() };

        var act = () => provider.GenerateTextAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*API key*");
    }

    [Fact]
    public async Task GenerateTextAsync_WhenApiReturnsError_ShouldThrow()
    {
        var factory = CreateFactory(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"error\":{\"message\":\"Invalid API key\"}}", Encoding.UTF8, "application/json")
        });

        var settingsMock = CreateSettingsMock(
            "https://my-resource.openai.azure.com",
            "bad-key",
            "gpt-4o");

        var provider = new AzureOpenAILLMProvider(factory, NullLogger<AzureOpenAILLMProvider>.Instance, settingsMock.Object);
        var request = new LLMRequest { Prompt = "Hello", TenantId = Guid.NewGuid() };

        var act = () => provider.GenerateTextAsync(request);
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
