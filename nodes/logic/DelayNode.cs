using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;

namespace OrchestAI.Nodes.Logic;

/// <summary>
/// Waits for a configurable duration before passing execution to the next node.
/// Useful for rate-limiting, debouncing, or simulating timed workflows.
/// </summary>
public sealed class DelayNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "logic.delay";

    /// <summary>
    /// Delays execution for the configured number of milliseconds, then outputs the actual elapsed time.
    /// </summary>
    /// <param name="ctx">Execution context providing the "durationMs" config.</param>
    /// <param name="ct">Cancellation token — cancels the delay if triggered.</param>
    /// <returns>Succeeded result with "delayedMs" output set to actual elapsed time.</returns>
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        // GetConfig returns double? for Number type; default to 1000ms if not set
        var durationMs = (int)(ctx.GetConfig<double?>("durationMs") ?? 1000.0);
        var sw = global::System.Diagnostics.Stopwatch.StartNew();
        await Task.Delay(durationMs, ct);
        sw.Stop();
        return NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["delayedMs"] = (double)sw.ElapsedMilliseconds });
    }
}

/// <summary>Descriptor for <see cref="DelayNode"/> — provides metadata for the designer palette.</summary>
public sealed class DelayNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "logic.delay";
    /// <inheritdoc />
    public string DisplayName => "Delay";
    /// <inheritdoc />
    public string Description => "Pauses execution for a specified number of milliseconds.";
    /// <inheritdoc />
    public string Category => "logic";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "clock";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("delayedMs", "Delayed Ms", "Actual milliseconds elapsed during the delay.", DataType.Number)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("durationMs", "Duration (ms)", "How long to wait in milliseconds.", DataType.Number, Required: true, DefaultValue: 1000)
    };
}
