namespace OrchestFlowAI.Application.Abstractions;

/// <summary>
/// Resolves {{secret:name}} placeholders in config values.
/// Used by the engine before passing config to each node.
/// </summary>
public interface ISecretService
{
    /// <summary>
    /// Replaces all {{secret:name}} tokens in the given string with the decrypted secret value.
    /// Returns the original string if no tokens found or secret not found.
    /// </summary>
    Task<string?> ResolveAsync(string? value, Guid tenantId, CancellationToken ct = default);

    /// <summary>Resolves all values in a config dictionary.</summary>
    Task<Dictionary<string, object?>> ResolveConfigAsync(Dictionary<string, object?> config, Guid tenantId, CancellationToken ct = default);
}
