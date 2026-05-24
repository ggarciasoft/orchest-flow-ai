using FluentAssertions;
using OrchestFlowAI.Domain.Enums;
using OrchestFlowAI.Infrastructure.Queue;

namespace OrchestFlowAI.Tests.QueueTests;

/// <summary>
/// Unit tests for <see cref="StubExecutionQueue"/> covering the full enqueue/dequeue/mark lifecycle.
/// </summary>
public sealed class StubExecutionQueueTests
{
    private static readonly Guid WorkflowId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public async Task EnqueueAsync_ShouldAddPendingItem()
    {
        var queue = new StubExecutionQueue();

        await queue.EnqueueAsync(WorkflowId, TenantId, "manual", "{}", CancellationToken.None);

        var item = await queue.DequeueAsync();
        item.Should().NotBeNull();
        item!.WorkflowId.Should().Be(WorkflowId);
        item.TenantId.Should().Be(TenantId);
        item.TriggeredBy.Should().Be("manual");
        item.Payload.Should().Be("{}");
    }

    [Fact]
    public async Task DequeueAsync_WhenEmpty_ShouldReturnNull()
    {
        var queue = new StubExecutionQueue();

        var item = await queue.DequeueAsync();

        item.Should().BeNull();
    }

    [Fact]
    public async Task DequeueAsync_ShouldTransitionItemToProcessing()
    {
        var queue = new StubExecutionQueue();
        await queue.EnqueueAsync(WorkflowId, TenantId, "webhook", "{\"event\":\"test\"}", CancellationToken.None);

        var item = await queue.DequeueAsync();

        item.Should().NotBeNull();
        item!.Status.Should().Be(ExecutionQueueItemStatus.Processing);
        item.PickedUpAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DequeueAsync_ShouldReturnItemsInFifoOrder()
    {
        var queue = new StubExecutionQueue();
        var wf1 = Guid.NewGuid();
        var wf2 = Guid.NewGuid();
        await queue.EnqueueAsync(wf1, TenantId, "manual", "{}", CancellationToken.None);
        await queue.EnqueueAsync(wf2, TenantId, "manual", "{}", CancellationToken.None);

        var first = await queue.DequeueAsync();
        var second = await queue.DequeueAsync();

        first!.WorkflowId.Should().Be(wf1);
        second!.WorkflowId.Should().Be(wf2);
    }

    [Fact]
    public async Task DequeueAsync_ShouldNotReturnSameItemTwice()
    {
        var queue = new StubExecutionQueue();
        await queue.EnqueueAsync(WorkflowId, TenantId, "cron", "{}", CancellationToken.None);

        var first = await queue.DequeueAsync();
        var second = await queue.DequeueAsync();

        first.Should().NotBeNull();
        second.Should().BeNull();
    }

    [Fact]
    public async Task MarkCompletedAsync_ShouldTransitionItemToDone()
    {
        var queue = new StubExecutionQueue();
        await queue.EnqueueAsync(WorkflowId, TenantId, "manual", "{}", CancellationToken.None);
        var item = await queue.DequeueAsync();
        item.Should().NotBeNull();

        await queue.MarkCompletedAsync(item!.Id);

        item.Status.Should().Be(ExecutionQueueItemStatus.Done);
        item.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkFailedAsync_ShouldTransitionItemToFailed()
    {
        var queue = new StubExecutionQueue();
        await queue.EnqueueAsync(WorkflowId, TenantId, "manual", "{}", CancellationToken.None);
        var item = await queue.DequeueAsync();
        item.Should().NotBeNull();

        await queue.MarkFailedAsync(item!.Id);

        item.Status.Should().Be(ExecutionQueueItemStatus.Failed);
        item.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkCompletedAsync_WithUnknownId_ShouldNotThrow()
    {
        var queue = new StubExecutionQueue();

        var act = async () => await queue.MarkCompletedAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MarkFailedAsync_WithUnknownId_ShouldNotThrow()
    {
        var queue = new StubExecutionQueue();

        var act = async () => await queue.MarkFailedAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }
}
