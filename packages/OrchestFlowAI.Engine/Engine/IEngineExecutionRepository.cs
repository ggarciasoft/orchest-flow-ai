using OrchestFlowAI.Domain.Entities;
namespace OrchestFlowAI.Engine;

public interface IEngineExecutionRepository
{
    Task<WorkflowExecution?> GetExecutionAsync(Guid id, CancellationToken ct = default);
    Task UpdateExecutionAsync(WorkflowExecution execution, CancellationToken ct = default);
    Task<WorkflowVersion?> GetWorkflowVersionAsync(Guid versionId, CancellationToken ct = default);
    /// <summary>Gets the workflow entity by id, used to retrieve retry policy and metadata.</summary>
    Task<Workflow?> GetWorkflowAsync(Guid workflowId, CancellationToken ct = default);
    Task<NodeExecution> CreateNodeExecutionAsync(NodeExecution nodeExecution, CancellationToken ct = default);
    Task UpdateNodeExecutionAsync(NodeExecution nodeExecution, CancellationToken ct = default);
    Task<NodeExecution?> GetNodeExecutionAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<NodeExecution>> GetNodeExecutionsAsync(Guid executionId, CancellationToken ct = default);
    Task<ApprovalRequest> CreateApprovalAsync(ApprovalRequest approval, CancellationToken ct = default);
}
