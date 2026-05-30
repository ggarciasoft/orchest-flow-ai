using Microsoft.Extensions.DependencyInjection;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.System;

/// <summary>
/// Pauses a workflow and waits for an external system to POST data via the webhook resume endpoint.
/// All fields in the posted JSON body are surfaced as node outputs for downstream nodes.
/// Unlike form nodes, there is no UI rendered — this is purely API-driven.
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
            // Surface all posted fields (plus the correlation metadata) as node outputs
            var resumeOutputs = new Dictionary<string, object?>(ctx.NodeInputs);
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
    };

    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("name",        "Checkpoint Name", "Descriptive label for this data checkpoint.",              DataType.String, Required: false),
        new NodeConfigDefinition("description", "Description",     "Documents what data the external system is expected to POST.", DataType.String, Required: false),
    };
}
