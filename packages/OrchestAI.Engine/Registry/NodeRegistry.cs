using OrchestAI.SDK.Interfaces;
using System.Collections.Concurrent;
namespace OrchestAI.Engine.Registry;

public sealed class NodeRegistry : INodeRegistry
{
    private readonly ConcurrentDictionary<string, IWorkflowNode> _nodes = new();
    private readonly ConcurrentDictionary<string, IWorkflowNodeDescriptor> _descriptors = new();

    public IWorkflowNode? GetNode(string type) => _nodes.GetValueOrDefault(type);
    public IWorkflowNodeDescriptor? GetDescriptor(string type) => _descriptors.GetValueOrDefault(type);
    public IReadOnlyCollection<IWorkflowNodeDescriptor> GetAllDescriptors() => _descriptors.Values.ToList().AsReadOnly();

    public void Register(IWorkflowNode node, IWorkflowNodeDescriptor descriptor)
    {
        _nodes[node.Type] = node;
        _descriptors[descriptor.Type] = descriptor;
    }
}