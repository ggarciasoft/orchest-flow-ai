using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using OrchestFlowAI.Nodes.Integrations;
using OrchestFlowAI.SDK.Models;
using OrchestFlowAI.SDK.Testing;

namespace OrchestFlowAI.Tests.NodeTests;

/// <summary>
/// Tests for all authentication modes of <see cref="HttpRequestNode"/>:
/// none, bearer, basic, api-key (header and query), and oauth2-client-credentials.
/// </summary>
public sealed class HttpRequestNodeAuthTests
{
    /// <summary>
    /// Captures the outgoing <see cref="HttpRequestMessage"/> so tests can assert headers/URI.
    /// </summary>
    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public HttpRequestMessage? Captured { get; private set; }

        public CaptureHandler(HttpStatusCode status = HttpStatusCode.OK, string body = "{}") =>
            (_status, _body) = (status, body);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Captured = request;
            return Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(_body) });
        }
    }

    private static (IServiceProvider Services, CaptureHandler Handler) BuildCapture(
        HttpStatusCode status = HttpStatusCode.OK, string body = "{}")
    {
        var handler = new CaptureHandler(status, body);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler));
        var services = new ServiceCollection().AddSingleton(factory.Object).BuildServiceProvider();
        return (services, handler);
    }

    /// <summary>
    /// Builds a capture handler that returns a fixed response for ALL requests (used for OAuth2
    /// where two requests are made: one token POST and one actual request).
    /// </summary>
    private static (IServiceProvider Services, List<HttpRequestMessage> Captured) BuildMultiCapture(
        Func<int, HttpResponseMessage> responseFactory)
    {
        var captured = new List<HttpRequestMessage>();
        var handlerMock = new Mock<HttpMessageHandler>();
        var callIndex = 0;
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                captured.Add(req);
                return responseFactory(callIndex++);
            });
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handlerMock.Object));
        var services = new ServiceCollection().AddSingleton(factory.Object).BuildServiceProvider();
        return (services, captured);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Auth: none
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_AuthNone_ShouldNotSetAuthorizationHeader()
    {
        var (services, handler) = BuildCapture();
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["url"] = "http://api.example.com/data", ["authType"] = "none" })
            .WithServices(services)
            .Build();

        var result = await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        handler.Captured!.Headers.Authorization.Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Auth: bearer
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_AuthBearer_ShouldSetBearerAuthorizationHeader()
    {
        var (services, handler) = BuildCapture();
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["url"] = "http://api.example.com/data", ["authType"] = "bearer", ["authToken"] = "my-secret-token" })
            .WithServices(services)
            .Build();

        await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);

        handler.Captured!.Headers.Authorization.Should().NotBeNull();
        handler.Captured.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.Captured.Headers.Authorization.Parameter.Should().Be("my-secret-token");
    }

    [Fact]
    public async Task Execute_AuthBearer_ShouldResolveTokenFromNodeInputPlaceholder()
    {
        var (services, handler) = BuildCapture();
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["url"] = "http://api.example.com/data", ["authType"] = "bearer", ["authToken"] = "{{token}}" })
            .WithInputs(new() { ["token"] = (object?)"resolved-token" })
            .WithServices(services)
            .Build();

        await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);

        handler.Captured!.Headers.Authorization!.Parameter.Should().Be("resolved-token");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Auth: basic
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_AuthBasic_ShouldSetBase64EncodedAuthorizationHeader()
    {
        var (services, handler) = BuildCapture();
        var ctx = new TestContextBuilder()
            .WithConfig(new()
            {
                ["url"] = "http://api.example.com/data",
                ["authType"] = "basic",
                ["authUsername"] = "alice",
                ["authPassword"] = "p@ssword"
            })
            .WithServices(services)
            .Build();

        await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);

        var expected = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("alice:p@ssword"));
        handler.Captured!.Headers.Authorization!.Scheme.Should().Be("Basic");
        handler.Captured.Headers.Authorization.Parameter.Should().Be(expected);
    }

    [Fact]
    public async Task Execute_AuthBasic_ShouldResolveUsernameAndPasswordFromPlaceholders()
    {
        var (services, handler) = BuildCapture();
        var ctx = new TestContextBuilder()
            .WithConfig(new()
            {
                ["url"] = "http://api.example.com/data",
                ["authType"] = "basic",
                ["authUsername"] = "{{user}}",
                ["authPassword"] = "{{pass}}"
            })
            .WithInputs(new() { ["user"] = (object?)"bob", ["pass"] = (object?)"secret" })
            .WithServices(services)
            .Build();

        await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);

        var expected = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("bob:secret"));
        handler.Captured!.Headers.Authorization!.Parameter.Should().Be(expected);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Auth: api-key (header)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_AuthApiKey_Header_ShouldAddKeyAsRequestHeader()
    {
        var (services, handler) = BuildCapture();
        var ctx = new TestContextBuilder()
            .WithConfig(new()
            {
                ["url"] = "http://api.example.com/data",
                ["authType"] = "api-key",
                ["authApiKeyName"] = "X-Api-Key",
                ["authApiKeyValue"] = "super-secret-key",
                ["authApiKeyLocation"] = "header"
            })
            .WithServices(services)
            .Build();

        await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);

        handler.Captured!.Headers.TryGetValues("X-Api-Key", out var values).Should().BeTrue();
        values!.Should().ContainSingle().Which.Should().Be("super-secret-key");
    }

    [Fact]
    public async Task Execute_AuthApiKey_Query_ShouldAppendKeyToQueryString()
    {
        var (services, handler) = BuildCapture();
        var ctx = new TestContextBuilder()
            .WithConfig(new()
            {
                ["url"] = "http://api.example.com/data",
                ["authType"] = "api-key",
                ["authApiKeyName"] = "apikey",
                ["authApiKeyValue"] = "abc123",
                ["authApiKeyLocation"] = "query"
            })
            .WithServices(services)
            .Build();

        await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);

        handler.Captured!.RequestUri!.ToString().Should().Contain("apikey=abc123");
    }

    [Fact]
    public async Task Execute_AuthApiKey_Query_ShouldAppendToExistingQueryString()
    {
        var (services, handler) = BuildCapture();
        var ctx = new TestContextBuilder()
            .WithConfig(new()
            {
                ["url"] = "http://api.example.com/data?foo=bar",
                ["authType"] = "api-key",
                ["authApiKeyName"] = "apikey",
                ["authApiKeyValue"] = "xyz",
                ["authApiKeyLocation"] = "query"
            })
            .WithServices(services)
            .Build();

        await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);

        var uri = handler.Captured!.RequestUri!.ToString();
        uri.Should().Contain("foo=bar");
        uri.Should().Contain("apikey=xyz");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Auth: oauth2-client-credentials
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_AuthOAuth2_ShouldFetchTokenAndAttachAsBearerHeader()
    {
        var tokenJson = """{"access_token":"fetched-access-token","token_type":"Bearer","expires_in":3600}""";
        var (services, captured) = BuildMultiCapture(i =>
            i == 0
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(tokenJson) }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("data") }
        );

        var ctx = new TestContextBuilder()
            .WithConfig(new()
            {
                ["url"] = "http://api.example.com/resource",
                ["authType"] = "oauth2-client-credentials",
                ["authTokenUrl"] = "http://auth.example.com/token",
                ["authClientId"] = "client-abc",
                ["authClientSecret"] = "secret-xyz",
                ["authScope"] = "read:data"
            })
            .WithServices(services)
            .Build();

        var result = await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);

        result.Status.Should().Be(NodeExecutionStatus.Succeeded);
        captured.Should().HaveCount(2, "one token request and one actual API request");

        // First call should be a POST to the token URL
        captured[0].Method.Should().Be(HttpMethod.Post);
        captured[0].RequestUri!.ToString().Should().Be("http://auth.example.com/token");

        // Second call should carry the Bearer token
        captured[1].Headers.Authorization!.Scheme.Should().Be("Bearer");
        captured[1].Headers.Authorization.Parameter.Should().Be("fetched-access-token");
    }

    [Fact]
    public async Task Execute_AuthOAuth2_TokenRequestFails_ShouldThrow()
    {
        var (services, _) = BuildMultiCapture(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("") }
        );

        var ctx = new TestContextBuilder()
            .WithConfig(new()
            {
                ["url"] = "http://api.example.com/resource",
                ["authType"] = "oauth2-client-credentials",
                ["authTokenUrl"] = "http://auth.example.com/token",
                ["authClientId"] = "bad-client",
                ["authClientSecret"] = "bad-secret",
                ["authScope"] = ""
            })
            .WithServices(services)
            .Build();

        var act = async () => await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Guard: unsupported auth type
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_InvalidAuthType_ShouldThrowInvalidOperationException()
    {
        var (services, _) = BuildCapture();
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["url"] = "http://api.example.com/data", ["authType"] = "unknown-auth-type" })
            .WithServices(services)
            .Build();

        var act = async () => await new HttpRequestNode().ExecuteAsync(ctx, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unsupported auth type*");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Descriptor
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Descriptor_ShouldExposeAllAuthTypeAllowedValues()
    {
        var descriptor = new HttpRequestNodeDescriptor();
        var authConfig = descriptor.Configuration.Single(c => c.Key == "authType");

        authConfig.AllowedValues.Should().BeEquivalentTo(
            new[] { "none", "bearer", "basic", "api-key", "oauth2-client-credentials" });
    }

    [Fact]
    public void Descriptor_ShouldHaveExpectedOutputs()
    {
        var descriptor = new HttpRequestNodeDescriptor();

        descriptor.Outputs.Should().HaveCount(3);
        descriptor.Outputs.Select(o => o.Key).Should()
            .BeEquivalentTo(new[] { "statusCode", "responseBody", "success" });
    }
}
