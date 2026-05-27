namespace OrchestFlowAI.SDK.Context;
public sealed class WorkflowExecutionContext
{
    public Guid ExecutionId { get; init; }
    public Guid WorkflowId { get; init; }
    public Guid WorkflowVersionId { get; init; }
    public Guid TenantId { get; init; }
    public Guid? TriggeredByUserId { get; init; }
    public string CorrelationId { get; init; } = default!;
    public IReadOnlyDictionary<string, object?> WorkflowInputs { get; init; } = new Dictionary<string, object?>();
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> NodeOutputs { get; init; } = new Dictionary<string, IReadOnlyDictionary<string, object?>>();
    public string CurrentNodeId { get; init; } = default!;
    public IReadOnlyDictionary<string, object?> NodeConfig { get; init; } = new Dictionary<string, object?>();
    public IReadOnlyDictionary<string, object?> NodeInputs { get; init; } = new Dictionary<string, object?>();
    public int Step { get; init; }
    public IServiceProvider Services { get; init; } = default!;
    public CancellationToken CancellationToken { get; init; }

    public T? GetInput<T>(string key)
    {
        if (NodeInputs.TryGetValue(key, out var val) && val is T typed) return typed;
        if (val is System.Text.Json.JsonElement je) return System.Text.Json.JsonSerializer.Deserialize<T>(je.GetRawText());
        try { return (T?)Convert.ChangeType(val, typeof(T)); } catch { return default; }
    }

    public T? GetConfig<T>(string key)
    {
        if (NodeConfig.TryGetValue(key, out var val) && val is T typed) return typed;
        if (val is System.Text.Json.JsonElement je) return System.Text.Json.JsonSerializer.Deserialize<T>(je.GetRawText());
        // Handle Nullable<T> — Convert.ChangeType does not support nullable types
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        // Handle string "true"/"false" → bool
        if (targetType == typeof(bool) && val is string boolStr)
        {
            if (bool.TryParse(boolStr, out var b)) return (T)(object)b;
            return default;
        }
        try { return (T?)Convert.ChangeType(val, targetType); } catch { return default; }
    }
}