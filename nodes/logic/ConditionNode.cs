using OrchestFlowAI.Engine.Conditions;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;
namespace OrchestFlowAI.Nodes.Logic;

/// <summary>
/// Evaluates a boolean expression against the node's input values and outputs the result.
/// Used to create conditional branches in a workflow (e.g. route based on a status field).
/// </summary>
public sealed class ConditionNode : IWorkflowNode
{
    private readonly ExpressionEvaluator _evaluator = new();

    /// <inheritdoc />
    public string Type => "logic.condition";

    /// <summary>
    /// Evaluates the configured boolean expression using the current node inputs as the scope.
    /// Outputs a single "result" boolean that downstream edges can branch on.
    /// </summary>
    /// <param name="ctx">Execution context providing config ("expression") and node inputs as scope variables.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Succeeded result with output key "result" set to true or false.</returns>
    /// <exception cref="InvalidOperationException">Thrown when "expression" config is missing.</exception>
    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var expression = ctx.GetConfig<string>("expression") ?? throw new InvalidOperationException("expression config is required");
        // Build the expression scope from all node inputs so variables like "status" resolve correctly
        var scope = ctx.NodeInputs.ToDictionary(kv => kv.Key, kv => kv.Value);
        var result = _evaluator.Evaluate(expression, scope);
        return Task.FromResult(NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["result"] = result }));
    }
}

/// <summary>Descriptor for the <see cref="ConditionNode"/> — provides metadata for the designer palette.</summary>
public sealed class ConditionNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "logic.condition";
    /// <inheritdoc />
    public string DisplayName => "Condition";
    /// <inheritdoc />
    public string Description => "Evaluates a boolean expression and routes to the matching branch.";
    /// <inheritdoc />
    public string Category => "logic";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "branch";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[] { new NodeOutputDefinition("result", "Result", "Boolean result of the condition.", DataType.Boolean) };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[] { new NodeConfigDefinition("expression", "Expression", "Boolean expression to evaluate.", DataType.String, Required: true) };
}
