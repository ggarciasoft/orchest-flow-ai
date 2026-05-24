using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Models;
namespace OrchestFlowAI.SDK.Interfaces;
public interface IWorkflowNode
{
    string Type { get; }
    Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken);
}