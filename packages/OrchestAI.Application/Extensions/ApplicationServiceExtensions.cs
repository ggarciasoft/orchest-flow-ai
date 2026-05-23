using Microsoft.Extensions.DependencyInjection;
namespace OrchestAI.Application.Extensions;
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddOrchestAIApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceExtensions).Assembly));
        return services;
    }
}