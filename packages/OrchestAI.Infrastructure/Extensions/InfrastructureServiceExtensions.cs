using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrchestAI.Application.Abstractions;
using OrchestAI.Infrastructure.Auth;
using OrchestAI.Infrastructure.Persistence;
using OrchestAI.Infrastructure.Queue;
using OrchestAI.Infrastructure.Repositories;
using OrchestAI.Infrastructure.Storage;

namespace OrchestAI.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering infrastructure services.
/// Automatically switches between PostgreSQL (when CONNECTION_STRING is set)
/// and in-memory stub repositories (for local dev without a database).
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all infrastructure dependencies: repositories, queue, storage, auth.
    /// </summary>
    public static IServiceCollection AddOrchestAIInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Shared services always registered
        services.AddSingleton<InMemoryExecutionQueue>();
        services.AddSingleton<IExecutionQueue>(sp => sp.GetRequiredService<InMemoryExecutionQueue>());
        services.AddScoped<IDocumentStorage, LocalFileDocumentStorage>();
        services.AddSingleton<JwtTokenService>();

        var connectionString = configuration.GetConnectionString("Default")
            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            // Real PostgreSQL repositories — used when CONNECTION_STRING is configured
            services.AddDbContext<OrchestAIDbContext>(opts =>
                opts.UseNpgsql(connectionString));

            services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();
            services.AddScoped<IExecutionRepository, EfExecutionRepository>();
            services.AddScoped<IApprovalRepository, EfApprovalRepository>();
            services.AddScoped<IUserRepository, EfUserRepository>();
            services.AddScoped<IDocumentRepository, EfDocumentRepository>();
            services.AddScoped<IAIUsageRepository, EfAIUsageRepository>();
            services.AddScoped<INodePresetRepository, EfNodePresetRepository>();
            services.AddScoped<OrchestAI.Engine.IEngineExecutionRepository, EfEngineExecutionRepository>();
        }
        else
        {
            // In-memory stub repositories — zero config, data lost on restart
            // TODO(BACKLOG#3): Replace with EF once PostgreSQL is provisioned
            services.AddScoped<IWorkflowRepository, StubWorkflowRepository>();
            services.AddScoped<IExecutionRepository, StubExecutionRepository>();
            services.AddScoped<IApprovalRepository, StubApprovalRepository>();
            services.AddScoped<IDocumentRepository, StubDocumentRepository>();
            services.AddScoped<IAIUsageRepository, StubAIUsageRepository>();
            services.AddScoped<IUserRepository, StubUserRepository>();
            services.AddScoped<INodePresetRepository, StubNodePresetRepository>();
            services.AddScoped<OrchestAI.Engine.IEngineExecutionRepository, StubEngineExecutionRepository>();
        }

        return services;
    }
}
