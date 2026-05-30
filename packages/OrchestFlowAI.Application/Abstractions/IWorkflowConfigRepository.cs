using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Application.Abstractions;

public interface IWorkflowConfigRepository
{
    Task<IReadOnlyList<WorkflowConfig>> ListAsync(Guid tenantId, CancellationToken ct = default);
    Task<WorkflowConfig?>              GetAsync(Guid tenantId, string key, CancellationToken ct = default);
    Task<WorkflowConfig>               CreateAsync(WorkflowConfig config, CancellationToken ct = default);
    Task                               UpdateAsync(WorkflowConfig config, CancellationToken ct = default);
    Task                               DeleteAsync(Guid tenantId, string key, CancellationToken ct = default);
}
