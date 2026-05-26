using System.Collections.Concurrent;
using OrchestFlowAI.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace OrchestFlowAI.Infrastructure.Settings;

/// <summary>
/// In-memory cached platform settings backed by the database.
/// Writes are committed to DB immediately and update the in-memory cache.
/// </summary>
public sealed class PlatformSettingsService : IPlatformSettingsService
{
    private readonly IServiceScopeFactory _scopeFactory;
    // Cache: tenantId:key -> value
    private readonly ConcurrentDictionary<string, string> _cache = new();

    public PlatformSettingsService(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public async Task<string?> GetAsync(Guid tenantId, string key, CancellationToken ct = default)
    {
        var cacheKey = $"{tenantId}:{key}";
        if (_cache.TryGetValue(cacheKey, out var cached)) return cached;

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPlatformSettingsRepository>();
        var setting = await repo.GetAsync(tenantId, key, ct);
        if (setting != null)
        {
            _cache[cacheKey] = setting.Value;
            return setting.Value;
        }
        return null;
    }

    public async Task SetAsync(Guid tenantId, string key, string value, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPlatformSettingsRepository>();
        await repo.UpsertAsync(tenantId, key, value, ct);
        _cache[$"{tenantId}:{key}"] = value;
    }

    public async Task<Dictionary<string, string>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPlatformSettingsRepository>();
        var settings = await repo.ListAsync(tenantId, ct);
        var dict = settings.ToDictionary(s => s.Key, s => s.Value);
        foreach (var kv in dict) _cache[$"{tenantId}:{kv.Key}"] = kv.Value;
        return dict;
    }
}
