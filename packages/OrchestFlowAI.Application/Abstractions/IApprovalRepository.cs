using OrchestFlowAI.Domain.Entities;
namespace OrchestFlowAI.Application.Abstractions;
public interface IApprovalRepository
{
    Task<ApprovalRequest?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<ApprovalRequest?> GetByNodeExecutionIdAsync(Guid nodeExecutionId, CancellationToken ct = default);
    Task<ApprovalRequest> CreateAsync(ApprovalRequest approval, CancellationToken ct = default);
    Task UpdateAsync(ApprovalRequest approval, CancellationToken ct = default);
    Task<IReadOnlyList<ApprovalRequest>> ListPendingAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);
}