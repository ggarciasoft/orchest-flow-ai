using OrchestAI.Domain.Entities;
namespace OrchestAI.Application.Abstractions;
public interface IWorkflowRepository
{
    Task<Workflow?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Workflow> CreateAsync(Workflow workflow, CancellationToken ct = default);
    Task UpdateAsync(Workflow workflow, CancellationToken ct = default);
    Task<IReadOnlyList<Workflow>> ListAsync(Guid tenantId, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, string? search, CancellationToken ct = default);
    Task<WorkflowVersion?> GetActiveVersionAsync(Guid workflowId, CancellationToken ct = default);
    Task<WorkflowVersion> CreateVersionAsync(WorkflowVersion version, CancellationToken ct = default);
    Task<WorkflowVersion?> GetVersionAsync(Guid versionId, CancellationToken ct = default);
    Task ActivateVersionAsync(Guid versionId, Guid workflowId, CancellationToken ct = default);
}