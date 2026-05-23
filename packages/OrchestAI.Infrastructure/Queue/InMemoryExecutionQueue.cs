using System.Threading.Channels;
using OrchestAI.Application.Abstractions;
using OrchestAI.Contracts.Events;
namespace OrchestAI.Infrastructure.Queue;

public sealed class InMemoryExecutionQueue : IExecutionQueue
{
    private readonly Channel<ExecutionQueueMessage> _execChannel = Channel.CreateUnbounded<ExecutionQueueMessage>();
    private readonly Channel<ExecutionResumeMessage> _resumeChannel = Channel.CreateUnbounded<ExecutionResumeMessage>();

    public Task EnqueueAsync(ExecutionQueueMessage message, CancellationToken ct = default)
    { _execChannel.Writer.TryWrite(message); return Task.CompletedTask; }

    public Task EnqueueResumeAsync(ExecutionResumeMessage message, CancellationToken ct = default)
    { _resumeChannel.Writer.TryWrite(message); return Task.CompletedTask; }

    public IAsyncEnumerable<ExecutionQueueMessage> ReadAllAsync(CancellationToken ct = default)
        => _execChannel.Reader.ReadAllAsync(ct);

    public IAsyncEnumerable<ExecutionResumeMessage> ReadAllResumeAsync(CancellationToken ct = default)
        => _resumeChannel.Reader.ReadAllAsync(ct);
}