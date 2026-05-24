using OrchestFlowAI.SDK.Models;
namespace OrchestFlowAI.SDK.Interfaces;

public interface IWorkflowNodeDescriptor
{
    string Type { get; }
    string DisplayName { get; }
    string Description { get; }
    string Category { get; }
    string Version { get; }
    string? IconKey { get; }
    IReadOnlyCollection<NodeInputDefinition> Inputs { get; }
    IReadOnlyCollection<NodeOutputDefinition> Outputs { get; }
    IReadOnlyCollection<NodeConfigDefinition> Configuration { get; }
}
