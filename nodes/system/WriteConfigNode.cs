using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.System;

/// <summary>
/// Writes a value to the tenant's persistent workflow config store.
/// Creates the key if it doesn't exist; updates it if it does.
/// The value can come from a node input (upstream output) or a literal config value.
/// </summary>
public sealed class WriteConfigNode : IWorkflowNode
{
    public string Type => "system.write-config";

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var key = ctx.GetConfig<string>("key")
            ?? throw new SDK.Exceptions.NodeExecutionException("CONFIG_MISSING_KEY", "key config is required.", retryable: false);

        var value = ctx.GetInput<string>("value") ?? ctx.GetConfig<string>("value") ?? "";
        var valueType = ctx.GetConfig<string>("valueType") ?? "string";

        var repo = ctx.Services.GetRequiredService<IWorkflowConfigRepository>();
        var existing = await repo.GetAsync(ctx.TenantId, key, ct);
        var previousValue = existing?.Value;

        if (existing == null)
        {
            var newEntry = OrchestFlowAI.Domain.Entities.WorkflowConfig.Create(ctx.TenantId, key, value, valueType);
            await repo.CreateAsync(newEntry, ct);
        }
        else
        {
            existing.Update(value, existing.Description);
            await repo.UpdateAsync(existing, ct);
        }

        return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
        {
            ["key"]           = key,
            ["newValue"]      = value,
            ["previousValue"] = previousValue,
        });
    }
}

/// <summary>Descriptor for <see cref="WriteConfigNode"/>.</summary>
public sealed class WriteConfigNodeDescriptor : IWorkflowNodeDescriptor
{
    public string  Type        => "system.write-config";
    public string  DisplayName => "Write Config";
    public string  Description => "Writes a value to the tenant config store. Creates if missing, updates if exists. Use to persist state between workflow runs.";
    public string  Category    => "system";
    public string  Version     => "1.0.0";
    public string? IconKey     => "layers";

    public IReadOnlyCollection<NodeInputDefinition> Inputs => new[]
    {
        new NodeInputDefinition("value", "Value", "Value to write (overrides the literal config value if provided).", DataType.String, Required: false),
    };
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("key",           "Key",            "The config key that was written.",        DataType.String),
        new NodeOutputDefinition("newValue",      "New Value",      "The value that was written.",             DataType.String),
        new NodeOutputDefinition("previousValue", "Previous Value", "The value before this write (or null).",  DataType.String),
    };
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("key",       "Config Key",  "The config key to write, e.g. gmail.last_sync_date.",     DataType.String, Required: true),
        new NodeConfigDefinition("value",     "Value",       "Literal value to write. Ignored if a value input is wired.", DataType.String, Required: false),
        new NodeConfigDefinition("valueType", "Value Type",  "Type hint: string | number | boolean | json | datetime.", DataType.Enum,   Required: false, DefaultValue: "string", AllowedValues: new[] { "string", "number", "boolean", "json", "datetime" }),
    };
}
