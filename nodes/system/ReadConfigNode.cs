using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;
using System.Globalization;

namespace OrchestFlowAI.Nodes.System;

/// <summary>
/// Reads a configuration value from the tenant's persistent workflow config store.
/// </summary>
public sealed class ReadConfigNode : IWorkflowNode
{
    public string Type => "system.read-config";

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var key = ctx.GetConfig<string>("key")
            ?? throw new SDK.Exceptions.NodeExecutionException("CONFIG_MISSING_KEY", "key config is required.", retryable: false);

        var repo = ctx.Services.GetRequiredService<IWorkflowConfigRepository>();
        var entry = await repo.GetAsync(ctx.TenantId, key, ct);

        if (entry == null)
        {
            var defaultValue = ctx.GetConfig<string>("defaultValue") ?? "";
            return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
            {
                ["value"]       = defaultValue,
                ["found"]       = false,
                ["key"]         = key,
                ["valueType"]   = "string",
            });
        }

        // Coerce the value to the declared type
        object? coerced = entry.ValueType switch
        {
            "number"   when double.TryParse(entry.Value, NumberStyles.Any,
                                            CultureInfo.InvariantCulture, out var d) => d,
            "boolean"  => entry.Value is "true" or "True" or "1",
            "json"     => entry.Value,
            "datetime" when DateTime.TryParse(entry.Value, null,
                                              DateTimeStyles.RoundtripKind, out var dt) => dt.ToString("O"),
            _          => entry.Value,
        };

        return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
        {
            ["value"]       = coerced,
            ["found"]       = true,
            ["key"]         = key,
            ["valueType"]   = entry.ValueType,
        });
    }
}

/// <summary>Descriptor for <see cref="ReadConfigNode"/>.</summary>
public sealed class ReadConfigNodeDescriptor : IWorkflowNodeDescriptor
{
    public string  Type        => "system.read-config";
    public string  DisplayName => "Read Config";
    public string  Description => "Reads a persistent configuration value from the tenant config store.";
    public string  Category    => "system";
    public string  Version     => "1.0.0";
    public string? IconKey     => "layers";

    public IReadOnlyCollection<NodeInputDefinition>  Inputs        => Array.Empty<NodeInputDefinition>();
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("value",     "Value",      "The config value (coerced to declared type).", DataType.String),
        new NodeOutputDefinition("found",     "Found",      "True if the key exists in the store.",          DataType.Boolean),
        new NodeOutputDefinition("key",       "Key",        "The config key that was read.",                 DataType.String),
        new NodeOutputDefinition("valueType", "Value Type", "The declared value type.",                      DataType.String),
    };
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("key",          "Config Key",     "The config key to read, e.g. gmail.last_sync_date.", DataType.String, Required: true),
        new NodeConfigDefinition("defaultValue", "Default Value",  "Value to use if the key does not exist.",            DataType.String, Required: false),
    };
}
