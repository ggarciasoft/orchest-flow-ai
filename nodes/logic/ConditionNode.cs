using OrchestAI.Engine.Conditions;
using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;
namespace OrchestAI.Nodes.Logic;

public sealed class ConditionNode : IWorkflowNode
{
    private readonly ExpressionEvaluator _evaluator = new();
    public string Type => "logic.condition";
    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var expression = ctx.GetConfig<string>("expression") ?? throw new InvalidOperationException("expression config is required");
        var scope = ctx.NodeInputs.ToDictionary(kv => kv.Key, kv => kv.Value);
        var result = _evaluator.Evaluate(expression, scope);
        return Task.FromResult(NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["result"] = result }));
    }
}

public sealed class ConditionNodeDescriptor : IWorkflowNodeDescriptor
{
    public string Type => "logic.condition";
    public string DisplayName => "Condition";
    public string Description => "Evaluates a boolean expression and routes to the matching branch.";
    public string Category => "logic";
    public string Version => "1.0.0";
    public string? IconKey => "branch";
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[] { new NodeOutputDefinition("result", "Result", "Boolean result of the condition.", DataType.Boolean) };
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[] { new NodeConfigDefinition("expression", "Expression", "Boolean expression to evaluate.", DataType.String, Required: true) };
}