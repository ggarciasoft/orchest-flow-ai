using OrchestAI.Infrastructure.Queue;
using OrchestAI.Contracts.Events;
using FluentAssertions;

namespace OrchestAI.Tests.InfrastructureTests;

public sealed class InMemoryExecutionQueueTests
{
    private static ExecutionQueueMessage MakeExecMsg() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid().ToString());

    private static ExecutionResumeMessage MakeResumeMsg() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Dictionary<string, object?> { ["decision"] = "approved" });

    [Fact]
    public async Task EnqueueAsync_AndRead_ShouldDeliverMessage()
    {
        var queue = new InMemoryExecutionQueue();
        var message = MakeExecMsg();

        await queue.EnqueueAsync(message);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var received = await queue.ReadAllAsync(cts.Token).FirstAsync();
        received.Should().BeEquivalentTo(message);
    }

    [Fact]
    public async Task EnqueueResumeAsync_AndRead_ShouldDeliverMessage()
    {
        var queue = new InMemoryExecutionQueue();
        var message = MakeResumeMsg();

        await queue.EnqueueResumeAsync(message);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var received = await queue.ReadAllResumeAsync(cts.Token).FirstAsync();
        received.Should().BeEquivalentTo(message);
    }

    [Fact]
    public async Task Enqueue_MultipleMessages_ShouldDeliverInOrder()
    {
        var queue = new InMemoryExecutionQueue();
        var messages = new[] { MakeExecMsg(), MakeExecMsg(), MakeExecMsg() };

        foreach (var m in messages)
            await queue.EnqueueAsync(m);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var results = new List<ExecutionQueueMessage>();
        await foreach (var msg in queue.ReadAllAsync(cts.Token))
        {
            results.Add(msg);
            if (results.Count == 3) break;
        }

        results.Select(r => r.ExecutionId).Should().BeEquivalentTo(
            messages.Select(m => m.ExecutionId), o => o.WithStrictOrdering());
    }
}
