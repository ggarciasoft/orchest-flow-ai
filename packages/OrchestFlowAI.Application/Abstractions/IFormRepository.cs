using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Application.Abstractions;

public interface IFormRepository
{
    Task<IReadOnlyList<Form>> ListAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Form>> ListAllAsync(CancellationToken ct = default);
    Task<Form?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Form?> GetBySlugAsync(string slug, Guid tenantId, CancellationToken ct = default);
    Task<Form> CreateAsync(Form form, CancellationToken ct = default);
    Task UpdateAsync(Form form, CancellationToken ct = default);
    Task<FormSubmission> CreateSubmissionAsync(FormSubmission submission, CancellationToken ct = default);
    Task<FormSubmission?> GetSubmissionByExecutionAsync(Guid executionId, string nodeExecutionId, CancellationToken ct = default);
}
