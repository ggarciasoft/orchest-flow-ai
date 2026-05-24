using System.Text.Json;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Events;
using StackExchange.Redis;

namespace OrchestFlowAI.Infrastructure.Queue;

/// <summary>
/// Redis-backed execution queue. Uses two list keys:
/// <c>OrchestFlowAI:queue:executions</c> and <c>OrchestFlowAI:queue:resumes</c>.
/// Enqueue pushes to the left; consume pops from the right (FIFO).
/// Falls back gracefully — workers poll every 500 ms when the queue is empty.
/// </summary>
public sealed class RedisExecutionQueue : IExecutionQueue, IExecutionQueueConsumer
{
    private const string ExecKey = "OrchestFlowAI:queue:executions";
    private const string ResumeKey = "OrchestFlowAI:queue:resumes";

    private readonly IConnectionMultiplexer _redis;

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    /// <summary>Initialises a new <see cref="RedisExecutionQueue"/> using the given Redis connection.</summary>
    public RedisExecutionQueue(IConnectionMultiplexer redis) => _redis = redis;

    /// <inheritdoc />
    public async Task EnqueueAsync(ExecutionQueueMessage message, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var payload = JsonSerializer.Serialize(message, _json);
        await db.ListLeftPushAsync(ExecKey, payload);
    }

    /// <inheritdoc />
    public async Task EnqueueResumeAsync(ExecutionResumeMessage message, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var payload = JsonSerializer.Serialize(message, _json);
        await db.ListLeftPushAsync(ResumeKey, payload);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ExecutionQueueMessage> ReadAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        while (!ct.IsCancellationRequested)
        {
            var value = await db.ListRightPopAsync(ExecKey);
            if (value.HasValue)
            {
                var msg = JsonSerializer.Deserialize<ExecutionQueueMessage>(value!, _json);
                if (msg is not null) yield return msg;
            }
            else
            {
                await Task.Delay(500, ct).ContinueWith(_ => { }, CancellationToken.None);
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ExecutionResumeMessage> ReadAllResumeAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        while (!ct.IsCancellationRequested)
        {
            var value = await db.ListRightPopAsync(ResumeKey);
            if (value.HasValue)
            {
                var msg = JsonSerializer.Deserialize<ExecutionResumeMessage>(value!, _json);
                if (msg is not null) yield return msg;
            }
            else
            {
                await Task.Delay(500, ct).ContinueWith(_ => { }, CancellationToken.None);
            }
        }
    }
}
