using Microsoft.Extensions.DependencyInjection;
using OrchestAI.Engine.Registry;
using OrchestAI.Nodes.AI;
using OrchestAI.Nodes.Documents;
using OrchestAI.Nodes.Human;
using OrchestAI.Nodes.Logic;
using OrchestAI.Nodes.System;
using OrchestAI.SDK.Interfaces;
namespace OrchestAI.Nodes;

public static class NodeRegistrationExtensions
{
    public static IServiceCollection AddOrchestAINodes(this IServiceCollection services)
    {
        // Register nodes + descriptors
        RegisterNode<SystemStartNode, SystemStartNodeDescriptor>(services);
        RegisterNode<SystemEndNode, SystemEndNodeDescriptor>(services);
        RegisterNode<ConditionNode, ConditionNodeDescriptor>(services);
        RegisterNode<HumanApprovalNode, HumanApprovalNodeDescriptor>(services);
        RegisterNode<ContractRiskAnalysisNode, ContractRiskAnalysisNodeDescriptor>(services);
        RegisterNode<ExecutiveSummaryNode, ExecutiveSummaryNodeDescriptor>(services);
        RegisterNode<ExtractPdfTextNode, ExtractPdfTextNodeDescriptor>(services);

        // Wire into registry after DI container is built
        services.AddHostedService<NodeRegistrationHostedService>();
        return services;
    }

    private static void RegisterNode<TNode, TDescriptor>(IServiceCollection services)
        where TNode : class, IWorkflowNode
        where TDescriptor : class, IWorkflowNodeDescriptor
    {
        services.AddSingleton<TNode>();
        services.AddSingleton<IWorkflowNode>(sp => sp.GetRequiredService<TNode>());
        services.AddSingleton<TDescriptor>();
        services.AddSingleton<IWorkflowNodeDescriptor>(sp => sp.GetRequiredService<TDescriptor>());
    }
}

public sealed class NodeRegistrationHostedService : Microsoft.Extensions.Hosting.IHostedService
{
    private readonly INodeRegistry _registry;
    private readonly IEnumerable<IWorkflowNode> _nodes;
    private readonly IEnumerable<IWorkflowNodeDescriptor> _descriptors;
    public NodeRegistrationHostedService(INodeRegistry registry, IEnumerable<IWorkflowNode> nodes, IEnumerable<IWorkflowNodeDescriptor> descriptors)
    { _registry = registry; _nodes = nodes; _descriptors = descriptors; }
    public Task StartAsync(CancellationToken ct)
    {
        var descMap = _descriptors.ToDictionary(d => d.Type);
        foreach (var node in _nodes)
        {
            if (descMap.TryGetValue(node.Type, out var desc))
                _registry.Register(node, desc);
        }
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}