using FluentAssertions;
using OrchestAI.Domain.Entities;
using OrchestAI.Domain.Enums;

namespace OrchestAI.Tests.DomainTests;

/// <summary>Unit tests for workflow trigger configuration via <see cref="Workflow.SetTrigger"/>.</summary>
public class WorkflowTriggerTests
{
    private static Workflow NewWorkflow() =>
        Workflow.Create(Guid.NewGuid(), "Test", "Desc", Guid.NewGuid());

    [Fact]
    public void Create_DefaultTriggerType_IsManual()
    {
        var workflow = NewWorkflow();
        workflow.TriggerType.Should().Be(TriggerType.Manual);
    }

    [Fact]
    public void Create_DefaultWebhookSecret_IsNull()
    {
        var workflow = NewWorkflow();
        workflow.WebhookSecret.Should().BeNull();
    }

    [Fact]
    public void Create_DefaultCronExpression_IsNull()
    {
        var workflow = NewWorkflow();
        workflow.CronExpression.Should().BeNull();
    }

    [Fact]
    public void Create_WithWebhookTrigger_SetsFieldsCorrectly()
    {
        var workflow = Workflow.Create(
            Guid.NewGuid(), "w", "d", Guid.NewGuid(),
            TriggerType.Webhook, webhookSecret: "my-secret");

        workflow.TriggerType.Should().Be(TriggerType.Webhook);
        workflow.WebhookSecret.Should().Be("my-secret");
        workflow.CronExpression.Should().BeNull();
    }

    [Fact]
    public void Create_WithCronTrigger_SetsFieldsCorrectly()
    {
        var workflow = Workflow.Create(
            Guid.NewGuid(), "w", "d", Guid.NewGuid(),
            TriggerType.Cron, cronExpression: "0 * * * *");

        workflow.TriggerType.Should().Be(TriggerType.Cron);
        workflow.CronExpression.Should().Be("0 * * * *");
        workflow.WebhookSecret.Should().BeNull();
    }

    [Fact]
    public void SetTrigger_ToWebhook_SetsSecretAndClearsExpression()
    {
        var workflow = NewWorkflow();
        workflow.SetTrigger(TriggerType.Webhook, webhookSecret: "s3cr3t", cronExpression: null);

        workflow.TriggerType.Should().Be(TriggerType.Webhook);
        workflow.WebhookSecret.Should().Be("s3cr3t");
        workflow.CronExpression.Should().BeNull();
    }

    [Fact]
    public void SetTrigger_ToCron_SetsExpressionAndClearsSecret()
    {
        var workflow = NewWorkflow();
        workflow.SetTrigger(TriggerType.Cron, webhookSecret: null, cronExpression: "*/5 * * * *");

        workflow.TriggerType.Should().Be(TriggerType.Cron);
        workflow.CronExpression.Should().Be("*/5 * * * *");
        workflow.WebhookSecret.Should().BeNull();
    }

    [Fact]
    public void SetTrigger_ToManual_ClearsBothFields()
    {
        var workflow = NewWorkflow();
        workflow.SetTrigger(TriggerType.Webhook, "secret", null);

        workflow.SetTrigger(TriggerType.Manual, null, null);

        workflow.TriggerType.Should().Be(TriggerType.Manual);
        workflow.WebhookSecret.Should().BeNull();
        workflow.CronExpression.Should().BeNull();
    }

    [Fact]
    public void SetTrigger_ToWebhook_WithoutSecret_ThrowsArgumentException()
    {
        var workflow = NewWorkflow();

        var act = () => workflow.SetTrigger(TriggerType.Webhook, webhookSecret: null, cronExpression: null);

        act.Should().Throw<ArgumentException>().WithMessage("*WebhookSecret*");
    }

    [Fact]
    public void SetTrigger_ToWebhook_WithEmptySecret_ThrowsArgumentException()
    {
        var workflow = NewWorkflow();

        var act = () => workflow.SetTrigger(TriggerType.Webhook, webhookSecret: "  ", cronExpression: null);

        act.Should().Throw<ArgumentException>().WithMessage("*WebhookSecret*");
    }

    [Fact]
    public void SetTrigger_ToCron_WithoutExpression_ThrowsArgumentException()
    {
        var workflow = NewWorkflow();

        var act = () => workflow.SetTrigger(TriggerType.Cron, webhookSecret: null, cronExpression: null);

        act.Should().Throw<ArgumentException>().WithMessage("*CronExpression*");
    }

    [Fact]
    public void SetTrigger_UpdatesUpdatedAt()
    {
        var workflow = NewWorkflow();
        var before = workflow.UpdatedAt;

        // Small delay to ensure time difference
        System.Threading.Thread.Sleep(5);
        workflow.SetTrigger(TriggerType.Webhook, "secret", null);

        workflow.UpdatedAt.Should().BeAfter(before);
    }
}
