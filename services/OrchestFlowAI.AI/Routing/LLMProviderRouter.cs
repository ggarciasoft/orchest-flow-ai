using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.Application.Abstractions;
namespace OrchestFlowAI.AI.Routing;

public sealed class LLMProviderRouter
{
    private readonly Dictionary<string, ILLMProvider> _providers;
    private readonly string _startupDefaultProvider;
    private readonly string _startupDefaultModel;
    private readonly IPlatformSettingsService? _settings;
    private readonly ITenantContext? _tenantContext;

    public LLMProviderRouter(
        IEnumerable<ILLMProvider> providers,
        string defaultProvider,
        string defaultModel,
        IPlatformSettingsService? settings = null,
        ITenantContext? tenantContext = null)
    {
        _providers = providers.ToDictionary(p => p.Id);
        _startupDefaultProvider = defaultProvider;
        _startupDefaultModel = defaultModel;
        _settings = settings;
        _tenantContext = tenantContext;
    }

    public (ILLMProvider Provider, string Model) Route(string? model)
    {
        var effectiveProvider = _startupDefaultProvider;
        var effectiveModel = _startupDefaultModel;

        if (_settings != null && _tenantContext != null)
        {
            try
            {
                var tenantId = _tenantContext.TenantId;
                var storedProvider = _settings.GetAsync(tenantId, "llm.defaultProvider", default).GetAwaiter().GetResult();
                var storedModel = _settings.GetAsync(tenantId, "llm.defaultModel", default).GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(storedProvider)) effectiveProvider = storedProvider;
                if (!string.IsNullOrWhiteSpace(storedModel)) effectiveModel = storedModel;
            }
            catch { }
        }

        if (model == null || model == "default")
            return (_providers.GetValueOrDefault(effectiveProvider) ?? _providers.Values.First(), effectiveModel);

        if (model.Contains('/'))
        {
            var parts = model.Split('/', 2);
            if (_providers.TryGetValue(parts[0], out var p)) return (p, parts[1]);
        }
        return (_providers.GetValueOrDefault(effectiveProvider) ?? _providers.Values.First(), model);
    }
}