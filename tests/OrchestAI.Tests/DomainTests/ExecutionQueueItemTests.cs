using FluentAssertions;
using OrchestAI.Domain.Entities;
using OrchestAI.Domain.Enums;

namespace OrchestAI.Tests.DomainTests;

/// <summary>
/// Unit tests for <see cref="ExecutionQueueItem"/> entity invariants and state transitions.
/// </summary>
public sealed class ExecutionQueueItemTests
{
    private static readonly Guid WorkflowId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldReturnPendingItemWithCorrectFields()
    {
        var before = DateTimeOffset.UtcNow;

        var item = ExecutionQueueItem.Create(WorkflowId, TenantId, "manual", "{\"key\":\"value\"}");

        item.Id.Should().NotBeEmpty();
        item.WorkflowId.Should().Be(WorkflowId);
        item.TenantId.Should().Be(TenantId);
        item.TriggeredBy.Should().Be("manual");
        item.Payload.Should().Be("{\"key\":\"value\"}");
        item.Status.Should().Be(ExecutionQueueItemStatus.Pending);
        item.CreatedAt.Should().BeOnOrAfter(before);
        item.PickedUpAt.Should().BeNull();
        item.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldAssignUniqueIds()
    {
        var item1 = ExecutionQueueItem.Create(WorkflowId, TenantId, "cron", "{}");
        var item2 = ExecutionQueueItem.Create(WorkflowId, TenantId, "cron", "{}");

        item1.Id.Should().NotBe(item2.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTriggeredBy_ShouldThrow(string triggeredBy)
    {
        var act = () => ExecutionQueueItem.Create(WorkflowId, TenantId, triggeredBy, "{}");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankPayload_ShouldThrow(string payload)
    {
        var act = () => ExecutionQueueItem.Create(WorkflowId, TenantId, "manual", payload);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkProcessing_WhenPending_ShouldTransitionToProcessing()
    {
        var item = ExecutionQueueItem.Create(WorkflowId, TenantId, "webhook", "{}");

        item.MarkProcessing();

        item.Status.Should().Be(ExecutionQueueItemStatus.Processing);
        item.PickedUpAt.Should().NotBeNull();
        item.PickedUpAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkProcessing_WhenNotPending_ShouldThrow()
    {
        var item = ExecutionQueueItem.Create(WorkflowId, TenantId, "manual", "{}");
        item.MarkProcessing();

        var act = () => item.MarkProcessing();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkCompleted_WhenProcessing_ShouldTransitionToDone()
    {
        var item = ExecutionQueueItem.Create(WorkflowId, TenantId, "manual", "{}");
        item.MarkProcessing();

        item.MarkCompleted();

        item.Status.Should().Be(ExecutionQueueItemStatus.Done);
        item.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkCompleted_WhenNotProcessing_ShouldThrow()
    {
        var item = ExecutionQueueItem.Create(WorkflowId, TenantId, "manual", "{}");

        var act = () => item.MarkCompleted();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkFailed_WhenProcessing_ShouldTransitionToFailed()
    {
        var item = ExecutionQueueItem.Create(WorkflowId, TenantId, "cron", "{}");
        item.MarkProcessing();

        item.MarkFailed();

        item.Status.Should().Be(ExecutionQueueItemStatus.Failed);
        item.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_WhenNotProcessing_ShouldThrow()
    {
        var item = ExecutionQueueItem.Create(WorkflowId, TenantId, "manual", "{}");

        var act = () => item.MarkFailed();

        act.Should().Throw<InvalidOperationException>();
    }
}
