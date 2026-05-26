namespace OrchestFlowAI.Application.Abstractions;

/// <summary>
/// Runtime-configurable platform settings with in-memory caching.
/// Changes take effect immediately without restart.
/// </summary>
public interface IPlatformSettingsService
{
    Task<string?> GetAsync(Guid tenantId, string key, CancellationToken ct = default);
    Task SetAsync(Guid tenantId, string key, string value, CancellationToken ct = default);
    Task<Dictionary<string, string>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
}
