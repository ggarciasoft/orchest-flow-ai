using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Application.Abstractions;

public interface ICorrelationTokenRepository
{
    Task<CorrelationToken> CreateAsync(CorrelationToken token, CancellationToken ct = default);
    Task<CorrelationToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task UpdateAsync(CorrelationToken token, CancellationToken ct = default);
}
