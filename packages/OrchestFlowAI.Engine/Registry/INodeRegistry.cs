using OrchestFlowAI.SDK.Interfaces;
namespace OrchestFlowAI.Engine.Registry;

public interface INodeRegistry
{
    IWorkflowNode? GetNode(string type);
    IWorkflowNodeDescriptor? GetDescriptor(string type);
    IReadOnlyCollection<IWorkflowNodeDescriptor> GetAllDescriptors();
    void Register(IWorkflowNode node, IWorkflowNodeDescriptor descriptor);
}