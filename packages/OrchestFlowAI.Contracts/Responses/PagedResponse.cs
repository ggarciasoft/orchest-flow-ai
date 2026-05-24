namespace OrchestFlowAI.Contracts.Responses;
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
