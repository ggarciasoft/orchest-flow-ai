namespace OrchestFlowAI.Engine;

/// <summary>
/// Minimal interface used by the engine to resolve {{secret:name}} tokens in node config.
/// Implemented by Infrastructure's SecretService; resolved via IServiceProvider.
/// </summary>
public interface ISecretResolver
{
    Task<Dictionary<string, object?>> ResolveConfigAsync(Dictionary<string, object?> config, Guid tenantId, CancellationToken ct = default);
}
