using OrchestAI.SDK.Interfaces;
namespace OrchestAI.Engine.Registry;

public interface INodeRegistry
{
    IWorkflowNode? GetNode(string type);
    IWorkflowNodeDescriptor? GetDescriptor(string type);
    IReadOnlyCollection<IWorkflowNodeDescriptor> GetAllDescriptors();
    void Register(IWorkflowNode node, IWorkflowNodeDescriptor descriptor);
}