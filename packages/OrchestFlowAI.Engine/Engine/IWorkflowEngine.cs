using OrchestFlowAI.Engine.Models;
using OrchestFlowAI.Engine.Validation;
namespace OrchestFlowAI.Engine;

public interface IWorkflowEngine
{
    Task<ValidationResult> ValidateAsync(WorkflowDefinition def, CancellationToken ct = default);
    Task RunAsync(Guid executionId, CancellationToken ct = default);
    Task ResumeAsync(Guid executionId, ResumeSignal signal, CancellationToken ct = default);
    Task CancelAsync(Guid executionId, CancellationToken ct = default);
}

public sealed record ResumeSignal(Guid NodeExecutionId, IReadOnlyDictionary<string, object?> ResumeOutputs);