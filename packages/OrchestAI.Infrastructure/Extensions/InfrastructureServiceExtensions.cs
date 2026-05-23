using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrchestAI.Application.Abstractions;
using OrchestAI.Infrastructure.Auth;
using OrchestAI.Infrastructure.Queue;
using OrchestAI.Infrastructure.Repositories;
using OrchestAI.Infrastructure.Storage;
namespace OrchestAI.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddOrchestAIInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<InMemoryExecutionQueue>();
        services.AddSingleton<IExecutionQueue>(sp => sp.GetRequiredService<InMemoryExecutionQueue>());
        services.AddScoped<IDocumentStorage, LocalFileDocumentStorage>();
        services.AddSingleton<JwtTokenService>();

        // Stub repositories (implement in next phase with EF Core)
        services.AddScoped<IWorkflowRepository, StubWorkflowRepository>();
        services.AddScoped<IExecutionRepository, StubExecutionRepository>();
        services.AddScoped<IApprovalRepository, StubApprovalRepository>();
        services.AddScoped<IDocumentRepository, StubDocumentRepository>();
        services.AddScoped<IAIUsageRepository, StubAIUsageRepository>();
        services.AddScoped<IUserRepository, StubUserRepository>();
        services.AddScoped<OrchestAI.Engine.IEngineExecutionRepository, StubEngineExecutionRepository>();
        return services;
    }
}