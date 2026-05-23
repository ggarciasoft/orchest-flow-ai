using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using OrchestAI.Nodes.Integrations;
using OrchestAI.SDK.Models;
using OrchestAI.SDK.Testing;

namespace OrchestAI.Tests.NodeTests;

/// <summary>Unit tests for <see cref="WebhookOutNode"/>.</summary>
public sealed class WebhookOutNodeTests
{
    private static IServiceProvider BuildServices(HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = status });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));

        return new ServiceCollection().AddSingleton(factory.Object).BuildServiceProvider();
    }

    [Fact]
    public async Task Execute_ShouldSendPayloadAndReturnSentTrue()
    {
        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["exampleInput"] = "Value1" })
            .WithConfig(new() { ["url"] = "http://example.com", ["includeInputs"] = "all" })
            .WithServices(BuildServices())
            .Build();

        var result = await new WebhookOutNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["sent"].Should().Be(true);
    }

    [Fact]
    public void Type_ShouldBeIntegrationsWebhookOut()
    {
        new WebhookOutNode().Type.Should().Be("integrations.webhook-out");
    }
}
