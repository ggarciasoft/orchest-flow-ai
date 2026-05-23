using Microsoft.Extensions.DependencyInjection;
using OrchestAI.Engine.Registry;
namespace OrchestAI.Engine.Extensions;

public static class EngineServiceExtensions
{
    public static IServiceCollection AddOrchestAIEngine(this IServiceCollection services)
    {
        services.AddSingleton<INodeRegistry, NodeRegistry>();
        services.AddScoped<IWorkflowEngine, WorkflowExecutionEngine>();
        return services;
    }
}