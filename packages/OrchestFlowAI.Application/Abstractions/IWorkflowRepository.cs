using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
namespace OrchestFlowAI.Application.Abstractions;
public interface IWorkflowRepository
{
    Task<Workflow?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Workflow?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Workflow> CreateAsync(Workflow workflow, CancellationToken ct = default);
    Task UpdateAsync(Workflow workflow, CancellationToken ct = default);
    Task<IReadOnlyList<Workflow>> ListAsync(Guid tenantId, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, string? search, CancellationToken ct = default);
    Task<WorkflowVersion?> GetActiveVersionAsync(Guid workflowId, CancellationToken ct = default);
    Task<WorkflowVersion> CreateVersionAsync(WorkflowVersion version, CancellationToken ct = default);
    Task<WorkflowVersion?> GetVersionAsync(Guid versionId, CancellationToken ct = default);
    Task ActivateVersionAsync(Guid versionId, Guid workflowId, CancellationToken ct = default);
    /// <summary>Returns all versions for a workflow ordered by version number descending.</summary>
    Task<IReadOnlyList<WorkflowVersion>> ListVersionsAsync(Guid workflowId, CancellationToken ct = default);

    /// <summary>Returns all non-deleted workflows with the specified trigger type.</summary>
    Task<IReadOnlyList<Workflow>> ListByTriggerTypeAsync(TriggerType triggerType, CancellationToken ct = default);
}