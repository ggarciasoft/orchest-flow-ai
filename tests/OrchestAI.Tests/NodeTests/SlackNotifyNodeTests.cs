using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using OrchestAI.Nodes.Integrations;
using OrchestAI.SDK.Models;
using OrchestAI.SDK.Testing;

namespace OrchestAI.Tests.NodeTests;

/// <summary>Unit tests for <see cref="SlackNotifyNode"/>.</summary>
public sealed class SlackNotifyNodeTests
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
    public async Task Execute_SuccessfulSend_ShouldReturnSentTrue()
    {
        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["user"] = "Alice" })
            .WithConfig(new() { ["webhookUrl"] = "http://example.com/hook", ["message"] = "Hello {{user}}" })
            .WithServices(BuildServices())
            .Build();

        var result = await new SlackNotifyNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["sent"].Should().Be(true);
    }

    [Fact]
    public void Type_ShouldBeIntegrationsSlack()
    {
        new SlackNotifyNode().Type.Should().Be("integrations.slack");
    }
}
