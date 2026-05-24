using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Infrastructure.Queue;

namespace OrchestFlowAI.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring infrastructure services.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddExecutionQueue(this IServiceCollection services)
    {
        var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
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

        return services;
    }
}