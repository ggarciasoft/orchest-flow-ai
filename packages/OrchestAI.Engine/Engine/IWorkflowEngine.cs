using OrchestAI.Engine.Models;
using OrchestAI.Engine.Validation;
namespace OrchestAI.Engine;

public interface IWorkflowEngine
{
    Task<ValidationResult> ValidateAsync(WorkflowDefinition def, CancellationToken ct = default);
    Task RunAsync(Guid executionId, CancellationToken ct = default);
    Task ResumeAsync(Guid executionId, ResumeSignal signal, CancellationToken ct = default);
}

public sealed record ResumeSignal(Guid NodeExecutionId, IReadOnlyDictionary<string, object?> ResumeOutputs);