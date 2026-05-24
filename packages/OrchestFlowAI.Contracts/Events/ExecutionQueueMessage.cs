namespace OrchestFlowAI.Contracts.Events;
public sealed record ExecutionQueueMessage(Guid ExecutionId, Guid TenantId, string CorrelationId);