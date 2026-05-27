using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.AI.Prompts;
using OrchestFlowAI.AI.Routing;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
namespace OrchestFlowAI.Nodes.AI;

public sealed record ContractRiskOutput(
    [property: JsonPropertyName("riskLevel")] string RiskLevel,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("keyClauses")] IReadOnlyList<ContractKeyClause> KeyClauses,
    [property: JsonPropertyName("recommendedAction")] string RecommendedAction);

public sealed record ContractKeyClause(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("risk")] string Risk,
    [property: JsonPropertyName("reason")] string Reason);

public sealed class ContractRiskAnalysisNode : IWorkflowNode
{
    public string Type => "ai.contract-risk-analysis";
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var text = ctx.GetInput<string>(ctx.GetConfig<string>("textInput") ?? "text")
            ?? (string.IsNullOrEmpty(ctx.GetConfig<string>("textInput")) || ctx.GetConfig<string>("textInput") == "text"
                ? ctx.GetInput<string>("item") ?? ctx.GetInput<string>("body") : null)
            ?? throw new InvalidOperationException($"Input '{ctx.GetConfig<string>("textInput") ?? "text"}' is required");
        var model = ctx.GetConfig<string>("model") ?? "default";
        var router = ctx.Services.GetRequiredService<LLMProviderRouter>();
        var (provider, resolvedModel) = router.Route(model);
        var schema = """{"type":"object","required":["riskLevel","summary","keyClauses","recommendedAction"],"properties":{"riskLevel":{"type":"string","enum":["Low","Medium","High"]},"summary":{"type":"string"},"keyClauses":{"type":"array","items":{"type":"object","properties":{"title":{"type":"string"},"risk":{"type":"string"},"reason":{"type":"string"}}}},"recommendedAction":{"type":"string"}}}""";
        var req = new LLMRequest { Prompt = ContractRiskPrompt.User(text), SystemPrompt = ContractRiskPrompt.System, Model = resolvedModel, MaxTokens = 1024, TenantId = ctx.TenantId };
        var response = await provider.GenerateStructuredAsync<ContractRiskOutput>(req, schema, ct);
        return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
        {
            ["riskLevel"] = response.Output.RiskLevel,
            ["summary"] = response.Output.Summary,
            ["keyClauses"] = JsonSerializer.Serialize(response.Output.KeyClauses),
            ["recommendedAction"] = response.Output.RecommendedAction
        });
    }
}

public sealed class ContractRiskAnalysisNodeDescriptor : IWorkflowNodeDescriptor
{
    public string Type => "ai.contract-risk-analysis";
    public string DisplayName => "AI Contract Risk Analysis";
    public string Description => "Analyzes a contract and returns a structured risk assessment.";
    public string Category => "ai";
    public string Version => "1.0.0";
    public string? IconKey => "shield-check";
    public IReadOnlyCollection<NodeInputDefinition> Inputs => new[] { new NodeInputDefinition("text", "Contract Text", "Full text of the contract.", DataType.String, Required: true) };
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("riskLevel", "Risk Level", "Low, Medium, or High", DataType.String),
        new NodeOutputDefinition("summary", "Summary", "AI-generated summary", DataType.String),
        new NodeOutputDefinition("keyClauses", "Key Clauses", "JSON array of key clauses", DataType.Json),
        new NodeOutputDefinition("recommendedAction", "Recommended Action", "What to do next", DataType.String)
    };
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("textInput", "Text Input", "Which upstream output to use as the text source.", DataType.Enum, Required: false, DefaultValue: "text", AllowedValues: new[] { "text", "item", "body" }),
        new NodeConfigDefinition("model", "Model", "LLM model to use.", DataType.String, Required: false, DefaultValue: "default", OptionsSource: "llm-models"),
        new NodeConfigDefinition("riskThreshold", "Risk Threshold", "Minimum risk level to flag.", DataType.Enum, Required: false, DefaultValue: "high", AllowedValues: new[] { "low", "medium", "high" })
    };
}
