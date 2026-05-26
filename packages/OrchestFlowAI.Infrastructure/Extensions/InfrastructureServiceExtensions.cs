using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Notifications;
using OrchestFlowAI.Infrastructure.Auth;
using OrchestFlowAI.Infrastructure.Persistence;
using OrchestFlowAI.Infrastructure.Queue;
using OrchestFlowAI.Infrastructure.Repositories;
using OrchestFlowAI.Infrastructure.Storage;
using OrchestFlowAI.Infrastructure.Notifications;
using OrchestFlowAI.Infrastructure.Settings;
using StackExchange.Redis;

namespace OrchestFlowAI.Infrastructure.Extensions;

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
    public static IServiceCollection AddOrchestFlowAIInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Queue — Redis when REDIS_URL is set, otherwise in-memory
        var redisUrl = configuration["Redis:Url"] ?? Environment.GetEnvironmentVariable("REDIS_URL");
        if (!string.IsNullOrEmpty(redisUrl))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisUrl));
            services.AddSingleton<RedisExecutionQueue>();
            services.AddSingleton<IExecutionQueue>(sp => sp.GetRequiredService<RedisExecutionQueue>());
            services.AddSingleton<IExecutionQueueConsumer>(sp => sp.GetRequiredService<RedisExecutionQueue>());
        }
        else
        {
            services.AddSingleton<InMemoryExecutionQueue>();
            services.AddSingleton<IExecutionQueue>(sp => sp.GetRequiredService<InMemoryExecutionQueue>());
            services.AddSingleton<IExecutionQueueConsumer>(sp => sp.GetRequiredService<InMemoryExecutionQueue>());
        }

        services.AddScoped<IDocumentStorage, LocalFileDocumentStorage>();
        services.AddSingleton<JwtTokenService>();

        // Default to stub notifier; API layer will override with SignalR when SignalR is configured
        services.AddScoped<IExecutionNotifier, StubExecutionNotifier>();

        // Persistent queue — PostgreSQL when CONNECTION_STRING is set, otherwise in-memory stub
        // (registered after the DB section so PostgresExecutionQueue can depend on the scoped DbContext)

        var connectionString = configuration.GetConnectionString("Default")
            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            // Real PostgreSQL repositories — used when CONNECTION_STRING is configured
            services.AddDbContext<OrchestFlowAIDbContext>(opts =>
                opts.UseNpgsql(connectionString));

            services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();
            services.AddScoped<IExecutionRepository, EfExecutionRepository>();
            services.AddScoped<IApprovalRepository, EfApprovalRepository>();
            services.AddScoped<IUserRepository, EfUserRepository>();
            services.AddScoped<IDocumentRepository, EfDocumentRepository>();
            services.AddScoped<IAIUsageRepository, EfAIUsageRepository>();
            services.AddScoped<INodePresetRepository, EfNodePresetRepository>();
            services.AddScoped<ITenantRepository, EfTenantRepository>();
            services.AddScoped<ITenantInviteRepository, EfTenantInviteRepository>();
            services.AddScoped<IGmailCredentialRepository, EfGmailCredentialRepository>();
            services.AddScoped<OrchestFlowAI.Engine.IEngineExecutionRepository, EfEngineExecutionRepository>();
            services.AddScoped<IPlatformSettingsRepository, EfPlatformSettingsRepository>();
            // Persistent queue backed by PostgreSQL
            services.AddScoped<IPersistentExecutionQueue, PostgresExecutionQueue>();
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
            services.AddScoped<ITenantRepository, StubTenantRepository>();
            services.AddScoped<ITenantInviteRepository, StubTenantInviteRepository>();
            services.AddScoped<IGmailCredentialRepository, StubGmailCredentialRepository>();
            services.AddScoped<OrchestFlowAI.Engine.IEngineExecutionRepository, StubEngineExecutionRepository>();
            services.AddScoped<IPlatformSettingsRepository, StubPlatformSettingsRepository>();
            // In-memory stub persistent queue — no DB required
            services.AddSingleton<IPersistentExecutionQueue, StubExecutionQueue>();
        }

        // Platform settings service — singleton with in-memory cache, backed by DB
        services.AddSingleton<IPlatformSettingsService, PlatformSettingsService>();

        return services;
    }
}
