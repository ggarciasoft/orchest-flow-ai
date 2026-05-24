using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace OrchestFlowAI.Tests.TestUtils;

/// <summary>Extension helpers for building mock-based service providers in tests.</summary>
public static class MockExtensions
{
    /// <summary>Adds a mocked IHttpClientFactory using the provided handler and returns the built ServiceProvider.</summary>
    public static IServiceProvider AddMockHttpClientFactory(this IServiceCollection services, HttpMessageHandler handler)
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler));
        services.AddSingleton(mockFactory.Object);
        return services.BuildServiceProvider();
    }
}
