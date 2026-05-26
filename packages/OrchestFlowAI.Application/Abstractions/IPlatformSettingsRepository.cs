using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Application.Abstractions;

public interface IPlatformSettingsRepository
{
    Task<IReadOnlyList<PlatformSetting>> ListAsync(Guid tenantId, CancellationToken ct = default);
    Task<PlatformSetting?> GetAsync(Guid tenantId, string key, CancellationToken ct = default);
    Task UpsertAsync(Guid tenantId, string key, string value, CancellationToken ct = default);
}
