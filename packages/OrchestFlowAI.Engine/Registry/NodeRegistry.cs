using OrchestFlowAI.SDK.Interfaces;
using System.Collections.Concurrent;
namespace OrchestFlowAI.Engine.Registry;

/// <summary>
/// Registry to manage workflow nodes and their descriptors.
/// Provides functionality to register, retrieve, and list all registered nodes.
/// </summary>
public sealed class NodeRegistry : INodeRegistry
{
    private readonly ConcurrentDictionary<string, IWorkflowNode> _nodes = new();
    private readonly ConcurrentDictionary<string, IWorkflowNodeDescriptor> _descriptors = new();

    public IWorkflowNode? GetNode(string type) => _nodes.GetValueOrDefault(type);
    public IWorkflowNodeDescriptor? GetDescriptor(string type) => _descriptors.GetValueOrDefault(type);
    public IReadOnlyCollection<IWorkflowNodeDescriptor> GetAllDescriptors() => _descriptors.Values.ToList().AsReadOnly();

    /// <summary>
    /// Registers a workflow node and its related descriptor.
    /// </summary>
    public void Register(IWorkflowNode node, IWorkflowNodeDescriptor descriptor)
    {
        _nodes[node.Type] = node;
        _descriptors[descriptor.Type] = descriptor;
    }

    /// <summary>Removes a node and its descriptor from the registry. No-op if not found.</summary>
    public void Unregister(string type)
    {
        _nodes.TryRemove(type, out _);
        _descriptors.TryRemove(type, out _);
    }
}
