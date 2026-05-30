using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;
using System.Globalization;
using System.Text.Json;

namespace OrchestFlowAI.Nodes.System;

/// <summary>
/// Pauses a workflow and waits for an external system to POST data via the webhook resume endpoint.
/// All fields in the posted JSON body are surfaced as node outputs for downstream nodes.
/// Unlike form nodes, there is no UI rendered â€” this is purely API-driven.
/// </summary>
public sealed class SystemDataCheckpointNode : IWorkflowNode
{
    public string Type => "system.data-checkpoint";

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        // Resume path: engine calls us again after the external system POSTed data.
        // The webhook controller always injects _resumedAt into the resume outputs.
        if (ctx.NodeInputs.ContainsKey("_resumedAt"))
        {
            var errors = new List<string>();
            var fieldsJson = ctx.GetConfig<string>("fields");
            
            if (!string.IsNullOrWhiteSpace(fieldsJson))
            {
                List<FieldDef> fieldDefs;
                try
                {
                    fieldDefs = JsonSerializer.Deserialize<List<FieldDef>>(
                        fieldsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new();
                }
                catch
                {
                    fieldDefs = new();
                }

                foreach (var field in fieldDefs)
                {
                    var hasValue = ctx.NodeInputs.TryGetValue(field.Key, out var rawValue)
                                   && rawValue is not null
                                   && rawValue.ToString() != string.Empty;

                    if (field.Required && !hasValue)
                    {
                        errors.Add($"Missing required field: '{field.Key}'");
                        continue;
                    }

                    if (!hasValue) continue;

                    var strVal = rawValue!.ToString()!;
                    var typeError = field.Type.ToLowerInvariant() switch
                    {
                        "number" when !double.TryParse(strVal, NumberStyles.Any,
                                                       CultureInfo.InvariantCulture, out _)
                            => $"Field '{field.Key}' must be a number (got '{strVal}')",
                        "boolean" when strVal is not ("true" or "false" or "True" or "False" or "1" or "0")
                            => $"Field '{field.Key}' must be a boolean (got '{strVal}')",
                        _ => null
                    };

                    if (typeError != null) errors.Add(typeError);
                }
            }

            if (errors.Count > 0)
            {
                var errorMsg = string.Join("; ", errors);
                return NodeExecutionResult.Failed($"Validation failed: {errorMsg}");
            }

            // Coerce typed fields in outputs
            var resumeOutputs = new Dictionary<string, object?>();
            var coerceFieldDefs = string.IsNullOrWhiteSpace(fieldsJson) ? new List<FieldDef>() :
                JsonSerializer.Deserialize<List<FieldDef>>(fieldsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            foreach (var kv in ctx.NodeInputs)
            {
                var fieldDef = coerceFieldDefs.FirstOrDefault(f =>
                    string.Equals(f.Key, kv.Key, StringComparison.OrdinalIgnoreCase));
                
                object? coercedValue = kv.Value;
                if (fieldDef != null && kv.Value is not null)
                {
                    var strVal = kv.Value.ToString()!;
                    coercedValue = fieldDef.Type.ToLowerInvariant() switch
                    {
                        "number" when double.TryParse(strVal, NumberStyles.Any,
                                                      CultureInfo.InvariantCulture, out var d) => d,
                        "boolean" => strVal is "true" or "True" or "1",
                        _ => kv.Value
                    };
                }
                resumeOutputs[kv.Key] = coercedValue;
            }

            resumeOutputs["_validationPassed"] = true;
            resumeOutputs["_validationErrors"] = "[]";
            return NodeExecutionResult.Succeeded(resumeOutputs);
        }

        // First execution: create a correlation token and suspend
        var tokenRepo = ctx.Services.GetRequiredService<ICorrelationTokenRepository>();

        var correlationToken = CorrelationToken.Create(
            ctx.ExecutionId,
            ctx.NodeExecutionId,
            ctx.TenantId,
            "data-checkpoint",
            null);

        await tokenRepo.CreateAsync(correlationToken, ct);

        var outputs = new Dictionary<string, object?>
        {
            ["_correlationToken"] = correlationToken.Token,
            ["_resumeUrl"]        = $"/api/webhooks/resume/{correlationToken.Token}",
        };

        return NodeExecutionResult.WaitingForApproval(outputs);
    }

    private sealed record FieldDef(string Key, string Type = "any", bool Required = false);
}

/// <summary>Descriptor for <see cref="SystemDataCheckpointNode"/>.</summary>
public sealed class SystemDataCheckpointNodeDescriptor : IWorkflowNodeDescriptor
{
    public string Type        => "system.data-checkpoint";
    public string DisplayName => "Data Checkpoint";
    public string Description => "Pauses execution and waits for an external system to POST data to the resume URL. All posted JSON fields become node outputs.";
    public string Category    => "system";
    public string Version     => "1.0.0";
    public string? IconKey    => "download";

    public IReadOnlyCollection<NodeInputDefinition>  Inputs        => Array.Empty<NodeInputDefinition>();

    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("_correlationToken", "Correlation Token", "One-time token for the resume URL.",      DataType.String),
        new NodeOutputDefinition("_resumeUrl",        "Resume URL",        "URL the external system POSTs data to.", DataType.String),
        new NodeOutputDefinition("_resumedAt",        "Resumed At",        "ISO timestamp when data was received.",  DataType.String),
        new NodeOutputDefinition("_validationPassed", "Validation Passed", "True when all field validations passed.", DataType.Boolean),
        new NodeOutputDefinition("_validationErrors", "Validation Errors", "JSON array of validation error messages, empty if none.", DataType.String),
    };

    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("name",        "Checkpoint Name", "Descriptive label for this data checkpoint.",              DataType.String, Required: false),
        new NodeConfigDefinition("description", "Description",     "Documents what data the external system is expected to POST.", DataType.String, Required: false),
        new NodeConfigDefinition(
            "fields",
            "Expected Fields",
            "JSON array defining expected fields. Example: [{\"key\":\"amount\",\"type\":\"number\",\"required\":true}]. Supported types: string, number, boolean, any.",
            DataType.String,
            Required: false,
            IsMultiline: true),
    };
}
