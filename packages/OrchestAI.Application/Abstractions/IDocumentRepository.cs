using OrchestAI.Domain.Entities;
namespace OrchestAI.Application.Abstractions;
public interface IDocumentRepository
{
    Task<Document?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Document> CreateAsync(Document document, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> ListByOwnerAsync(Guid ownerId, Guid tenantId, CancellationToken ct = default);
}