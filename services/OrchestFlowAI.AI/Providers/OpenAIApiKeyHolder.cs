namespace OrchestFlowAI.AI.Providers;

/// <summary>
/// Mutable holder for the OpenAI API key, allowing runtime updates
/// without restarting the application.
/// </summary>
public sealed class OpenAIApiKeyHolder
{
    private string _apiKey;

    public OpenAIApiKeyHolder(string initialApiKey)
        => _apiKey = initialApiKey;

    public string ApiKey => _apiKey;

    public void Update(string newKey)
        => Interlocked.Exchange(ref _apiKey, newKey);
}
