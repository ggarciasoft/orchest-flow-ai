using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Application.Abstractions;

public interface IAiChatRepository
{
    Task<AiChatSession>                CreateSessionAsync(AiChatSession session, CancellationToken ct = default);
    Task<AiChatSession?>               GetSessionAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AiChatSession>> ListSessionsAsync(Guid tenantId, string? surface = null, Guid? contextId = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task                               UpdateSessionAsync(AiChatSession session, CancellationToken ct = default);

    Task<AiChatMessage>                AddMessageAsync(AiChatMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<AiChatMessage>> GetMessagesAsync(Guid sessionId, CancellationToken ct = default);
}
