using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
namespace OrchestFlowAI.Observability;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddOrchestFlowAIObservability(this IServiceCollection services)
    {
        services.AddSingleton<CorrelationIdMiddleware>();
        return services;
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}