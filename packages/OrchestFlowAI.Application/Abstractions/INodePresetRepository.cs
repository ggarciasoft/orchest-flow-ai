using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Application.Abstractions;

public interface INodePresetRepository
{
    Task<NodePreset?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<NodePreset>> ListByNodeTypeAsync(Guid tenantId, string? nodeType, CancellationToken ct = default);
    Task<NodePreset> CreateAsync(NodePreset preset, CancellationToken ct = default);
    Task UpdateAsync(NodePreset preset, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
}