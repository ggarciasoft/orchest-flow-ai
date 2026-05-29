using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.AI.Abstractions;
using OrchestFlowAI.AI.Routing;

namespace OrchestFlowAI.Api.Services;

public sealed record FormGenerationRequest(
    string Prompt,
    string? CurrentFieldsJson,
    string? FormName,
    string? FormDescription
);

public sealed record FormGenerationResult(
    string FieldsJson,
    string Explanation,
    IReadOnlyList<string> Changes
);

/// <summary>
/// Uses the configured LLM to generate or modify custom form field definitions
/// based on a natural-language prompt.
/// </summary>
public sealed class FormGenerationService
{
    private readonly LLMProviderRouter _router;
    private readonly ILogger<FormGenerationService> _logger;

    public FormGenerationService(LLMProviderRouter router, ILogger<FormGenerationService> logger)
    {
        _router = router;
        _logger = logger;
    }

    public async Task<FormGenerationResult> GenerateAsync(
        FormGenerationRequest req,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var llmRequest = new LLMRequest
        {
            SystemPrompt = BuildSystemPrompt(),
            Prompt = BuildUserPrompt(req),
            Model = "default",
            Temperature = 0.2,
            MaxTokens = 2048,
            TenantId = tenantId
        };

        var (provider, model) = _router.Route("default");
        var response = await provider.GenerateTextAsync(llmRequest with { Model = model }, ct);
        _logger.LogInformation("FormGenerationService: LLM tokens used: {Total}", response.Usage.TotalTokens);

        return ParseResponse(response.Text);
    }

    private static string BuildSystemPrompt() =>
        "You are an expert form builder assistant for OrchestFlowAI.\n" +
        "Generate or modify form field definitions based on user requests.\n\n" +
        "FIELD SCHEMA (each element in the fields array):\n" +
        "{\n" +
        "  \"key\": \"<snake_case identifier, no spaces>\",\n" +
        "  \"label\": \"<human-readable label>\",\n" +
        "  \"type\": \"text\" | \"number\" | \"select\" | \"date\" | \"email\" | \"boolean\",\n" +
        "  \"required\": true | false,\n" +
        "  \"placeholder\": \"<optional hint text>\",\n" +
        "  \"options\": [\"<opt1>\", \"<opt2>\"]   // only for type=select\n" +
        "}\n\n" +
        "RULES:\n" +
        "- Use snake_case for keys (e.g. first_name, invoice_date)\n" +
        "- Keep labels concise and human-friendly\n" +
        "- Use select when there is a finite known set of options\n" +
        "- Use boolean for yes/no or toggle fields\n" +
        "- Return ONLY a JSON object with exactly these fields:\n" +
        "  { \"explanation\": \"<what you did>\", \"changes\": [\"<change1>\", ...], \"fields\": [ ... ] }\n" +
        "- The fields array must be valid JSON and non-empty.\n" +
        "- Do NOT wrap the response in markdown code fences.";

    private static string BuildUserPrompt(FormGenerationRequest req)
    {
        var sb = new StringBuilder();
        var name = req.FormName ?? "Untitled Form";
        if (req.CurrentFieldsJson == null || req.CurrentFieldsJson == "[]" || req.CurrentFieldsJson.Trim() == "")
            sb.AppendLine($"Create fields for a form called \"{name}\".");
        else
            sb.AppendLine($"Modify the fields for the form \"{name}\".");

        if (!string.IsNullOrWhiteSpace(req.FormDescription))
            sb.AppendLine($"Form purpose: {req.FormDescription}");

        sb.AppendLine($"Request: {req.Prompt}");

        if (!string.IsNullOrWhiteSpace(req.CurrentFieldsJson) && req.CurrentFieldsJson != "[]")
            sb.AppendLine($"Current fields:\n{req.CurrentFieldsJson}");

        return sb.ToString();
    }

    private static FormGenerationResult ParseResponse(string rawText)
    {
        var text = rawText.Trim();
        if (text.StartsWith("```"))
        {
            var nl = text.IndexOf('\n');
            if (nl >= 0) text = text[(nl + 1)..];
            if (text.EndsWith("```")) text = text[..^3].TrimEnd();
        }

        JsonDocument doc;
        try { doc = JsonDocument.Parse(text); }
        catch (JsonException ex)
            { throw new InvalidOperationException($"LLM response was not valid JSON: {rawText}", ex); }

        var root = doc.RootElement;

        if (!root.TryGetProperty("explanation", out var explanationEl))
            throw new InvalidOperationException($"LLM response missing 'explanation': {rawText}");
        if (!root.TryGetProperty("fields", out var fieldsEl) || fieldsEl.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"LLM response missing 'fields' array: {rawText}");

        root.TryGetProperty("changes", out var changesEl);
        var changes = changesEl.ValueKind == JsonValueKind.Array
            ? changesEl.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s != "").ToList()
            : new List<string>();

        return new FormGenerationResult(
            fieldsEl.GetRawText(),
            explanationEl.GetString() ?? string.Empty,
            changes);
    }
}
