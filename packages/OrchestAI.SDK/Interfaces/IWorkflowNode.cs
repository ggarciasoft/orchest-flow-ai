using OrchestAI.SDK.Context;
using OrchestAI.SDK.Models;
namespace OrchestAI.SDK.Interfaces;
public interface IWorkflowNode
{
    string Type { get; }
    Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken);
}