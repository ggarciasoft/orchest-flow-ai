using OrchestAI.Domain.Entities;
namespace OrchestAI.Application.Abstractions;
public interface IExecutionRepository
{
    Task<WorkflowExecution?> GetAsync(Guid id, CancellationToken ct = default);
    Task<WorkflowExecution> CreateAsync(WorkflowExecution execution, CancellationToken ct = default);
    Task UpdateAsync(WorkflowExecution execution, CancellationToken ct = default);
    Task<IReadOnlyList<WorkflowExecution>> ListAsync(Guid tenantId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<NodeExecution>> GetNodeExecutionsAsync(Guid executionId, CancellationToken ct = default);
    Task<NodeExecution> CreateNodeExecutionAsync(NodeExecution nodeExecution, CancellationToken ct = default);
    Task UpdateNodeExecutionAsync(NodeExecution nodeExecution, CancellationToken ct = default);
    Task<NodeExecution?> GetNodeExecutionAsync(Guid id, CancellationToken ct = default);
}