using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.Engine.Registry;
using OrchestFlowAI.Nodes.Integrations;
using OrchestFlowAI.Nodes.AI;
using OrchestFlowAI.Nodes.Documents;
using OrchestFlowAI.Nodes.Human;
using OrchestFlowAI.Nodes.Logic;
using OrchestFlowAI.Nodes.Data;
using OrchestFlowAI.Nodes.System;
using OrchestFlowAI.SDK.Interfaces;
namespace OrchestFlowAI.Nodes;

public static class NodeRegistrationExtensions
{
    public static IServiceCollection AddOrchestFlowAINodes(this IServiceCollection services)
    {
        // Register nodes + descriptors
        RegisterNode<SystemStartNode, SystemStartNodeDescriptor>(services);
        RegisterNode<SystemEndNode, SystemEndNodeDescriptor>(services);
        RegisterNode<SystemDataCheckpointNode, SystemDataCheckpointNodeDescriptor>(services);
        RegisterNode<ReadConfigNode, ReadConfigNodeDescriptor>(services);
        RegisterNode<WriteConfigNode, WriteConfigNodeDescriptor>(services);
        RegisterNode<ConditionNode, ConditionNodeDescriptor>(services);
        RegisterNode<DelayNode, DelayNodeDescriptor>(services);
        RegisterNode<SwitchNode, SwitchNodeDescriptor>(services);
        RegisterNode<MergeNode, MergeNodeDescriptor>(services);
        RegisterNode<HumanApprovalNode, HumanApprovalNodeDescriptor>(services);
        RegisterNode<ContractRiskAnalysisNode, ContractRiskAnalysisNodeDescriptor>(services);
RegisterNode<TextClassifierNode, TextClassifierNodeDescriptor>(services);
RegisterNode<DataExtractorNode, DataExtractorNodeDescriptor>(services);
RegisterNode<TranslationNode, TranslationNodeDescriptor>(services);
        RegisterNode<ExecutiveSummaryNode, ExecutiveSummaryNodeDescriptor>(services);
        RegisterNode<ExtractPdfTextNode, ExtractPdfTextNodeDescriptor>(services);
        RegisterNode<SelectDocumentNode, SelectDocumentNodeDescriptor>(services);
        RegisterNode<SetVariableNode, SetVariableNodeDescriptor>(services);
        RegisterNode<JsonTransformNode, JsonTransformNodeDescriptor>(services);
        RegisterNode<DatabaseQueryNode, DatabaseQueryNodeDescriptor>(services);
        RegisterNode<DatabaseExecuteNode, DatabaseExecuteNodeDescriptor>(services);

        // Wire into registry after DI container is built
        RegisterNode<ForEachNode, ForEachNodeDescriptor>(services);
        RegisterNode<ForEachEndNode, ForEachEndNodeDescriptor>(services);
        RegisterNode<GmailReadNode, GmailReadNodeDescriptor>(services);
        RegisterNode<HttpRequestNode, HttpRequestNodeDescriptor>(services);
        RegisterNode<SlackNotifyNode, SlackNotifyNodeDescriptor>(services);
        RegisterNode<WebhookOutNode, WebhookOutNodeDescriptor>(services);
        RegisterNode<SendEmailNode, SendEmailNodeDescriptor>(services);
        RegisterNode<WaitForWebhookNode, WaitForWebhookNodeDescriptor>(services);
        RegisterNode<ExternalGateNode, ExternalGateNodeDescriptor>(services);

        services.AddHttpClient();
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