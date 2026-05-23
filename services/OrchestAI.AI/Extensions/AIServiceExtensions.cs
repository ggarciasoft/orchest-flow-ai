using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchestAI.AI.Abstractions;
using OrchestAI.AI.Providers;
using OrchestAI.AI.Routing;
namespace OrchestAI.AI.Extensions;

public static class AIServiceExtensions
{
    public static IServiceCollection AddOrchestAIAI(this IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = configuration["LLM:OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("ORCHESTAI_LLM__OPENAI__API_KEY") ?? "";
        var defaultProvider = configuration["LLM:DefaultProvider"] ?? "openai";
        var defaultModel = configuration["LLM:DefaultModel"] ?? "gpt-4o-mini";

        services.AddSingleton<ILLMProvider>(sp => new OpenAILLMProvider(apiKey, sp.GetRequiredService<ILogger<OpenAILLMProvider>>()));
        services.AddSingleton(sp => new LLMProviderRouter(sp.GetServices<ILLMProvider>(), defaultProvider, defaultModel));
        return services;
    }
}