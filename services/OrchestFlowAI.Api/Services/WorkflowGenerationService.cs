using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.AI.Routing;
using OrchestFlowAI.AI.Abstractions;
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
    IReadOnlyList<string> Changes
);

public sealed class WorkflowGenerationService
{
    private readonly LLMProviderRouter _router;
    private readonly INodeRegistry _nodeRegistry;
    private readonly ILogger<WorkflowGenerationService> _logger;

    public WorkflowGenerationService(
        LLMProviderRouter router,
        INodeRegistry nodeRegistry,
        ILogger<WorkflowGenerationService> logger)
    {
        _router = router;
        _nodeRegistry = nodeRegistry;
        _logger = logger;
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

        return ParseResponse(response.Text);
    }

    private string BuildSystemPrompt()
    {
        var catalogSection = BuildCatalogSection();

        return $"""
You are an expert workflow automation engineer for OrchestFlowAI.
Generate or modify workflow definitions based on user requests.

WORKFLOW DEFINITION SCHEMA:
{{
  "id": "<guid or keep existing>",
  "name": "<workflow name>",
  "version": 1,
  "nodes": [
    {{
      "id": "<type>-<unix-ms>",
      "type": "<node-type>",
      "position": {{"x": 350, "y": <150 * step>}},
      "data": {{"label": "<displayName>", "config": {{<key>: <value>}}}},
      "config": {{<key>: <value>}}
    }}
  ],
  "edges": [
    {{"id": "edge-<source>-<target>", "source": "<source-id>", "target": "<target-id>"}}
  ]
}}

RULES:
- Always include a system.start node (id: "system.start-1") as first node
- Always include a system.end node (id: "system.end-999") as last node
- Node IDs must be unique — use format "<type>-<unix-ms>" with incrementing values
- Position nodes top-to-bottom: x=350, y increments by 150 per step
- Edges must reference valid node IDs
- config and data.config must have identical contents
- For ForEach loops: loopMode must be true, wire: foreach -> body nodes -> foreach.end -> rest
- Return ONLY a JSON object with these fields: explanation, changes, definition

AVAILABLE NODES:
{catalogSection}
""";
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
            return $"""
Create a new workflow called "{name}".
Request: {req.Prompt}
""";
        }
        else
        {
            return $"""
Update the following workflow.
Request: {req.Prompt}
Current definition:
{req.CurrentDefinitionJson}
""";
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
                var s = item.GetString();
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

        return new WorkflowGenerationResult(definition, explanation, changes);
    }
}
