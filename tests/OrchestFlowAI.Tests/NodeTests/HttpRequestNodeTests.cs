using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using OrchestFlowAI.Nodes.Integrations;
using OrchestFlowAI.SDK.Models;
using OrchestFlowAI.SDK.Testing;

namespace OrchestFlowAI.Tests.NodeTests;

/// <summary>Unit tests for <see cref="HttpRequestNode"/>.</summary>
public sealed class HttpRequestNodeTests
{
    private static IServiceProvider BuildServices(HttpStatusCode status, string responseBody = "")
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = status, Content = new StringContent(responseBody) });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));

        return new ServiceCollection()
            .AddSingleton(factory.Object)
            .BuildServiceProvider();
    }

    [Fact]
    public async Task Execute_SuccessResponse_ShouldReturnStatusAndBody()
    {
        var services = BuildServices(HttpStatusCode.OK, "Success Response");
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["url"] = "http://example.com", ["method"] = "GET" })
            .WithServices(services)
            .Build();

        var result = await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["responseBody"].Should().Be("Success Response");
        result.Outputs["statusCode"].Should().Be(200);
        result.Outputs["success"].Should().Be(true);
    }

    [Fact]
    public async Task Execute_FailureResponse_Default_ShouldReturnSucceededWithSuccessFalse()
    {
        var services = BuildServices(HttpStatusCode.InternalServerError, "Error");
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["url"] = "http://example.com", ["method"] = "GET" })
            .WithServices(services)
            .Build();

        var result = await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["success"].Should().Be(false);
        result.Outputs["statusCode"].Should().Be(500);
    }

    [Fact]
    public async Task Execute_FailureResponse_FailOnError_ShouldReturnFailed()
    {
        var services = BuildServices(HttpStatusCode.BadRequest, "Bad Request Error");
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["url"] = "http://example.com", ["method"] = "GET", ["failOnError"] = true })
            .WithServices(services)
            .Build();

        var result = await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Failed);
        result.ErrorMessage.Should().Contain("400");
        result.Outputs["success"].Should().Be(false);
        result.Outputs["statusCode"].Should().Be(400);
        result.Outputs["responseBody"].Should().Be("Bad Request Error");
    }

    [Fact]
    public async Task Execute_FailureResponse_FailOnError_ExplicitFalse_ShouldSucceed()
    {
        var services = BuildServices(HttpStatusCode.InternalServerError, "Error");
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["url"] = "http://example.com", ["method"] = "GET", ["failOnError"] = false })
            .WithServices(services)
            .Build();

        var result = await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        result.Outputs["success"].Should().Be(false);
        result.Outputs["statusCode"].Should().Be(500);
    }

    [Fact]
    public void Type_ShouldBeIntegrationsHttp()
    {
        new HttpRequestNode().Type.Should().Be("integrations.http");
    }
}
