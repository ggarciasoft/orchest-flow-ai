using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.AI.Routing;
using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Engine.Registry;

namespace OrchestFlowAI.Api.Services;

public sealed record WorkflowGenerationRequest(
    string Prompt,
    string? CurrentDefinitionJson,
    string? WorkflowName
);

public sealed record WorkflowGenerationResult(
    object Definition,
    string Explanation,
    IReadOnlyList<string> Changes,
    string Provider,
    string Model,
    int TotalTokens
);

public sealed class WorkflowGenerationService
{
    private readonly LLMProviderRouter _router;
    private readonly INodeRegistry _nodeRegistry;
    private readonly ILogger<WorkflowGenerationService> _logger;
    private readonly IServiceScopeFactory? _scopeFactory;

    public WorkflowGenerationService(
        LLMProviderRouter router,
        INodeRegistry nodeRegistry,
        ILogger<WorkflowGenerationService> logger,
        IServiceScopeFactory? scopeFactory = null)
    {
        _router = router;
        _nodeRegistry = nodeRegistry;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<WorkflowGenerationResult> GenerateAsync(
        WorkflowGenerationRequest req,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(req);

        var llmRequest = new LLMRequest
        {
            SystemPrompt = systemPrompt,
            Prompt = userPrompt,
            Model = "default",
            Temperature = 0.2,
            MaxTokens = 4096,
            TenantId = tenantId
        };

        var (provider, model) = _router.Route("default");
        var response = await provider.GenerateTextAsync(llmRequest with { Model = model }, ct);

        _logger.LogInformation("LLM response received, tokens used: {Total}", response.Usage.TotalTokens);

        if (_scopeFactory != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var chatRepo = scope.ServiceProvider.GetRequiredService<IAiChatRepository>();
                    
                    var session = AiChatSession.Create(tenantId, Guid.Empty, "workflow-assist");
                    await chatRepo.CreateSessionAsync(session);
                    await chatRepo.AddMessageAsync(AiChatMessage.CreateUserMessage(session.Id, userPrompt));
                    await chatRepo.AddMessageAsync(AiChatMessage.CreateAssistantMessage(
                        session.Id, response.Text, provider.Id, model,
                        response.Usage.PromptTokens, response.Usage.CompletionTokens));
                    session.Touch();
                    await chatRepo.UpdateSessionAsync(session);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to log AI chat history for workflow generation");
                }
            }, CancellationToken.None);
        }

        var parsed = ParseResponse(response.Text);
        return parsed with
        {
            Provider = provider.Id,
            Model    = model,
            TotalTokens = response.Usage.TotalTokens
        };
    }

    private string BuildSystemPrompt()
    {
        var catalogSection = BuildCatalogSection();

        return
            "You are an expert workflow automation engineer for OrchestFlowAI.\n" +
            "Generate or modify workflow definitions based on user requests.\n\n" +
            "WORKFLOW DEFINITION SCHEMA:\n" +
            "{\n" +
            "  \"id\": \"<guid or keep existing>\",\n" +
            "  \"name\": \"<workflow name>\",\n" +
            "  \"version\": 1,\n" +
            "  \"nodes\": [\n" +
            "    {\n" +
            "      \"id\": \"<type>-<unix-ms>\",\n" +
            "      \"type\": \"<node-type>\",\n" +
            "      \"position\": {\"x\": 350, \"y\": <150 * step>},\n" +
            "      \"data\": {\"label\": \"<displayName>\", \"config\": {<key>: <value>}},\n" +
            "      \"config\": {<key>: <value>}\n" +
            "    }\n" +
            "  ],\n" +
            "  \"edges\": [\n" +
            "    {\"id\": \"edge-<source>-<target>\", \"source\": \"<source-id>\", \"target\": \"<target-id>\"}\n" +
            "  ]\n" +
            "}\n\n" +
            "RULES:\n" +
            "- Always include a system.start node (id: \"system.start-1\") as first node\n" +
            "- Always include a system.end node (id: \"system.end-999\") as last node\n" +
            "- Node IDs must be unique — use format \"<type>-<unix-ms>\" with incrementing values\n" +
            "- Position nodes top-to-bottom: x=350, y increments by 150 per step\n" +
            "- Edges must reference valid node IDs\n" +
            "- config and data.config must have identical contents\n" +
            "- For ForEach loops: loopMode must be true, wire: foreach -> body nodes -> foreach.end -> rest\n" +
            "- Return ONLY a JSON object with these fields: explanation, changes, definition\n\n" +
            "AVAILABLE NODES:\n" +
            catalogSection;
    }

    private string BuildCatalogSection()
    {
        var sb = new StringBuilder();
        foreach (var d in _nodeRegistry.GetAllDescriptors())
        {
            var inputs = string.Join(", ", d.Inputs.Select(i => $"{i.Key}{(i.Required ? "(required)" : "")}"));
            var outputs = string.Join(", ", d.Outputs.Select(o => o.Key));
            var configs = string.Join(", ", d.Configuration.Take(5).Select(c => $"{c.Key}({c.Type})"));
            sb.AppendLine($"{d.Type} | {d.DisplayName} | inputs: {inputs} | outputs: {outputs} | config: {configs}");
        }
        return sb.ToString();
    }

    private static string BuildUserPrompt(WorkflowGenerationRequest req)
    {
        if (req.CurrentDefinitionJson == null)
        {
            var name = req.WorkflowName ?? "New Workflow";
            return $"Create a new workflow called \"{name}\".\nRequest: {req.Prompt}";
        }
        else
        {
            return $"Update the following workflow.\nRequest: {req.Prompt}\nCurrent definition:\n{req.CurrentDefinitionJson}";
        }
    }

    private static WorkflowGenerationResult ParseResponse(string rawText)
    {
        // Strip markdown fences if present
        var text = rawText.Trim();
        if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline >= 0)
                text = text[(firstNewline + 1)..];
            if (text.EndsWith("```"))
                text = text[..^3].TrimEnd();
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(text);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"LLM response was not valid JSON. Raw text: {rawText}", ex);
        }

        var root = doc.RootElement;

        if (!root.TryGetProperty("explanation", out var explanationEl))
            throw new InvalidOperationException($"LLM response missing 'explanation'. Raw text: {rawText}");

        if (!root.TryGetProperty("definition", out var definitionEl))
            throw new InvalidOperationException($"LLM response missing 'definition'. Raw text: {rawText}");

        root.TryGetProperty("changes", out var changesEl);

        var explanation = explanationEl.GetString() ?? string.Empty;

        var changes = new List<string>();
        if (changesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in changesEl.EnumerateArray())
            {
                var s = item.ValueKind switch
                {
                    JsonValueKind.String => item.GetString(),
                    JsonValueKind.Null   => null,
                    _                    => item.GetRawText(), // object, array, number → raw JSON
                };
                if (s != null) changes.Add(s);
            }
        }

        // Validate: must have nodes array including system.start and system.end
        if (!definitionEl.TryGetProperty("nodes", out var nodesEl) || nodesEl.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Definition missing 'nodes' array. Raw text: {rawText}");

        var nodeTypes = nodesEl.EnumerateArray()
            .Where(n => n.TryGetProperty("type", out _))
            .Select(n => n.GetProperty("type").GetString())
            .ToHashSet();

        if (!nodeTypes.Contains("system.start"))
            throw new InvalidOperationException($"Definition missing system.start node. Raw text: {rawText}");

        if (!nodeTypes.Contains("system.end"))
            throw new InvalidOperationException($"Definition missing system.end node. Raw text: {rawText}");

        // Return definition as object (deserialized from JsonElement)
        var definition = JsonSerializer.Deserialize<object>(definitionEl.GetRawText())!;

        return new WorkflowGenerationResult(definition, explanation, changes, "", "", 0);
    }
}
