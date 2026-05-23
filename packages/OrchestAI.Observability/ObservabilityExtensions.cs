using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
namespace OrchestAI.Observability;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddOrchestAIObservability(this IServiceCollection services)
    {
        services.AddSingleton<CorrelationIdMiddleware>();
        return services;
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}