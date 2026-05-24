using OrchestFlowAI.AI.Abstractions;
namespace OrchestFlowAI.AI.Routing;

public sealed class LLMProviderRouter
{
    private readonly Dictionary<string, ILLMProvider> _providers;
    private readonly string _defaultProvider;
    private readonly string _defaultModel;

    public LLMProviderRouter(IEnumerable<ILLMProvider> providers, string defaultProvider, string defaultModel)
    {
        _providers = providers.ToDictionary(p => p.Id);
        _defaultProvider = defaultProvider;
        _defaultModel = defaultModel;
    }

    public (ILLMProvider Provider, string Model) Route(string? model)
    {
        if (model == null || model == "default")
            return (_providers.GetValueOrDefault(_defaultProvider) ?? _providers.Values.First(), _defaultModel);

        if (model.Contains('/'))
        {
            var parts = model.Split('/', 2);
            if (_providers.TryGetValue(parts[0], out var p)) return (p, parts[1]);
        }
        return (_providers.GetValueOrDefault(_defaultProvider) ?? _providers.Values.First(), model);
    }
}