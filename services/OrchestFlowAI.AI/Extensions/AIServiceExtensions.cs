using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.AI.Providers;
using OrchestFlowAI.AI.Routing;
namespace OrchestFlowAI.AI.Extensions;

/// <summary>
/// Extension methods for registering AI/LLM services into the DI container.
/// </summary>
public static class AIServiceExtensions
{
    /// <summary>
    /// Registers LLM providers and the router.
    /// Set LLM_PROVIDER=fake in environment/config for local dev without an API key.
    /// Set LLM_PROVIDER=openai (default) with OPENAI_API_KEY for real calls.
    /// </summary>
    public static IServiceCollection AddOrchestFlowAIAI(this IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = configuration["LLM:OpenAI:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? "";

        var defaultProvider = configuration["LLM:DefaultProvider"]
            ?? Environment.GetEnvironmentVariable("LLM_PROVIDER")
            ?? "openai";

        var defaultModel = configuration["LLM:DefaultModel"]
            ?? Environment.GetEnvironmentVariable("OPENAI_DEFAULT_MODEL")
            ?? "gpt-4o-mini";

        // Ensure IHttpClientFactory is available (safe to call multiple times)
        services.AddHttpClient();

        // Always register FakeLLMProvider so it's available for tests and dev mode
        services.AddSingleton<FakeLLMProvider>();
        services.AddSingleton<ILLMProvider>(sp => sp.GetRequiredService<FakeLLMProvider>());

        // Always register the key holder so SettingsController can update it
        var holder = new OpenAIApiKeyHolder(apiKey);
        services.AddSingleton(holder);
        services.AddSingleton<OpenAILLMProvider>(sp =>
            new OpenAILLMProvider(holder, sp.GetRequiredService<ILogger<OpenAILLMProvider>>(),
                sp.GetService<OrchestFlowAI.Application.Abstractions.IPlatformSettingsService>()));
        services.AddSingleton<ILLMProvider>(sp => sp.GetRequiredService<OpenAILLMProvider>());

        // Anthropic
        services.AddSingleton<AnthropicLLMProvider>(sp =>
            new AnthropicLLMProvider(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ILogger<AnthropicLLMProvider>>(),
                sp.GetService<OrchestFlowAI.Application.Abstractions.IPlatformSettingsService>()));
        services.AddSingleton<ILLMProvider>(sp => sp.GetRequiredService<AnthropicLLMProvider>());

        // Azure OpenAI
        services.AddSingleton<AzureOpenAILLMProvider>(sp =>
            new AzureOpenAILLMProvider(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ILogger<AzureOpenAILLMProvider>>(),
                sp.GetService<OrchestFlowAI.Application.Abstractions.IPlatformSettingsService>()));
        services.AddSingleton<ILLMProvider>(sp => sp.GetRequiredService<AzureOpenAILLMProvider>());

        // Ollama
        services.AddSingleton<OllamaLLMProvider>(sp =>
            new OllamaLLMProvider(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ILogger<OllamaLLMProvider>>(),
                sp.GetService<OrchestFlowAI.Application.Abstractions.IPlatformSettingsService>()));
        services.AddSingleton<ILLMProvider>(sp => sp.GetRequiredService<OllamaLLMProvider>());

        // Router resolves the correct provider based on the model string or default
        services.AddSingleton(sp =>
            new LLMProviderRouter(
                sp.GetServices<ILLMProvider>(),
                defaultProvider,
                defaultModel,
                sp.GetService<OrchestFlowAI.Application.Abstractions.IPlatformSettingsService>(),
                sp.GetService<OrchestFlowAI.Application.Abstractions.ITenantContext>()));

        return services;
    }
}
