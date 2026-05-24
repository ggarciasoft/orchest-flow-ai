using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.Engine.Registry;
namespace OrchestFlowAI.Engine.Extensions;

public static class EngineServiceExtensions
{
    public static IServiceCollection AddOrchestFlowAIEngine(this IServiceCollection services)
    {
        services.AddSingleton<INodeRegistry, NodeRegistry>();
        services.AddScoped<IWorkflowEngine, WorkflowExecutionEngine>();
        return services;
    }
}