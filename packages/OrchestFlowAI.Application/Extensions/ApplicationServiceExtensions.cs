using Microsoft.Extensions.DependencyInjection;
namespace OrchestFlowAI.Application.Extensions;
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddOrchestFlowAIApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceExtensions).Assembly));
        return services;
    }
}