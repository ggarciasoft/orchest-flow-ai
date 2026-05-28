using OrchestFlowAI.SDK.Interfaces;
namespace OrchestFlowAI.Engine.Registry;

public interface INodeRegistry
{
    IWorkflowNode? GetNode(string type);
    IWorkflowNodeDescriptor? GetDescriptor(string type);
    IReadOnlyCollection<IWorkflowNodeDescriptor> GetAllDescriptors();
    void Register(IWorkflowNode node, IWorkflowNodeDescriptor descriptor);
    /// <summary>Removes a node and its descriptor from the registry. No-op if not found.</summary>
    void Unregister(string type);
}