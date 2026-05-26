using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.AI.Prompts;
using OrchestFlowAI.AI.Routing;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;
using Microsoft.Extensions.DependencyInjection;
namespace OrchestFlowAI.Nodes.AI;

public sealed class ExecutiveSummaryNode : IWorkflowNode
{
    public string Type => "ai.executive-summary";
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var text = ctx.GetInput<string>("text") ?? throw new InvalidOperationException("Input 'text' is required");
        var model = ctx.GetConfig<string>("model") ?? "default";
        var maxWords = ctx.GetConfig<int?>("maxWords") ?? 250;
        var tone = ctx.GetConfig<string>("tone") ?? "formal";
        var router = ctx.Services.GetRequiredService<LLMProviderRouter>();
        var (provider, resolvedModel) = router.Route(model);
        var req = new LLMRequest { Prompt = ExecutiveSummaryPrompt.User(text, maxWords, tone), SystemPrompt = ExecutiveSummaryPrompt.System, Model = resolvedModel, MaxTokens = 512 };
        var response = await provider.GenerateTextAsync(req, ct);
        return NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["summary"] = response.Text.Trim() });
    }
}

public sealed class ExecutiveSummaryNodeDescriptor : IWorkflowNodeDescriptor
{
    public string Type => "ai.executive-summary";
    public string DisplayName => "AI Executive Summary";
    public string Description => "Generates a concise executive summary of input text.";
    public string Category => "ai";
    public string Version => "1.0.0";
    public string? IconKey => "file-text";
    public IReadOnlyCollection<NodeInputDefinition> Inputs => new[] { new NodeInputDefinition("text", "Text", "Text to summarize.", DataType.String, Required: true) };
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[] { new NodeOutputDefinition("summary", "Summary", "Generated executive summary.", DataType.String) };
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("model", "Model", "LLM model.", DataType.String, Required: false, DefaultValue: "default", OptionsSource: "llm-models"),
        new NodeConfigDefinition("maxWords", "Max Words", "Maximum words in summary.", DataType.Number, Required: false, DefaultValue: 250),
        new NodeConfigDefinition("tone", "Tone", "Summary tone.", DataType.Enum, Required: false, DefaultValue: "formal", AllowedValues: new[] { "formal", "neutral", "friendly" })
    };
}
