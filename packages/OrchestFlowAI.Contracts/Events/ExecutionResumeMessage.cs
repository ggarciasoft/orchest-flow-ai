namespace OrchestFlowAI.Contracts.Events;
public sealed record ExecutionResumeMessage(Guid ExecutionId, Guid ApprovalId, Guid NodeExecutionId, Dictionary<string, object?> ResumeOutputs);