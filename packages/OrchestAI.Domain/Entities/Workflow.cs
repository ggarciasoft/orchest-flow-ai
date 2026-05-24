using OrchestAI.Domain.Enums;
using OrchestAI.Domain.ValueObjects;
namespace OrchestAI.Domain.Entities;

/// <summary>
/// Represents a workflow definition owned by a tenant.
/// </summary>
public sealed class Workflow
{
    /// <summary>
    /// Gets the unique identifier of the workflow.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the tenant that owns the workflow.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the name of the workflow.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Gets the description of the workflow.
    /// </summary>
    public string Description { get; private set; } = default!;

    /// <summary>
    /// Gets the unique identifier of the user who created the workflow.
    /// </summary>
    public Guid CreatedBy { get; private set; }

    /// <summary>
    /// Gets the date and time when the workflow was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the date and time when the workflow was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the trigger type that determines how this workflow is started.
    /// </summary>
    public TriggerType TriggerType { get; private set; } = TriggerType.Manual;

    /// <summary>
    /// Gets the shared secret used to verify inbound webhook requests.
    /// Only relevant when <see cref="TriggerType"/> is <see cref="TriggerType.Webhook"/>.
    /// </summary>
    public string? WebhookSecret { get; private set; }

    /// <summary>
    /// Gets the cron expression that defines the execution schedule.
    /// Only relevant when <see cref="TriggerType"/> is <see cref="TriggerType.Cron"/>.
    /// </summary>
    public string? CronExpression { get; private set; }

    /// <summary>
    /// Gets the retry policy applied to node executions within this workflow.
    /// </summary>
    public RetryPolicy RetryPolicy { get; private set; } = RetryPolicy.None;

    /// <summary>
    /// Indicates whether the workflow is deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets the date and time when the workflow was deleted, if applicable.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Workflow"/> class.
    /// </summary>
    private Workflow() { }

    /// <summary>
    /// Creates a new workflow with the given details.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant that owns the workflow.</param>
    /// <param name="name">The human-readable name of the workflow.</param>
    /// <param name="description">The description of the workflow.</param>
    /// <param name="createdBy">The unique identifier of the user who created the workflow.</param>
    /// <returns>A new <see cref="Workflow"/> instance with a generated identifier.</returns>
    /// <param name="triggerType">How the workflow will be triggered. Defaults to <see cref="TriggerType.Manual"/>.</param>
    /// <param name="webhookSecret">Optional shared secret for webhook verification.</param>
    /// <param name="cronExpression">Optional cron expression for scheduled execution.</param>
    public static Workflow Create(
        Guid tenantId,
        string name,
        string description,
        Guid createdBy,
        TriggerType triggerType = TriggerType.Manual,
        string? webhookSecret = null,
        string? cronExpression = null)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TriggerType = triggerType,
            WebhookSecret = webhookSecret,
            CronExpression = cronExpression,
        };

    /// <summary>
    /// Sets the trigger configuration for the workflow.
    /// </summary>
    /// <param name="triggerType">The type of trigger to configure.</param>
    /// <param name="webhookSecret">The shared secret for webhook verification. Required when <paramref name="triggerType"/> is <see cref="TriggerType.Webhook"/>.</param>
    /// <param name="cronExpression">The cron expression defining the schedule. Required when <paramref name="triggerType"/> is <see cref="TriggerType.Cron"/>.</param>
    /// <exception cref="ArgumentException">Thrown when required fields for the trigger type are missing.</exception>
    public void SetTrigger(TriggerType triggerType, string? webhookSecret, string? cronExpression)
    {
        if (triggerType == TriggerType.Webhook && string.IsNullOrWhiteSpace(webhookSecret))
            throw new ArgumentException("WebhookSecret is required for Webhook trigger type.", nameof(webhookSecret));
        if (triggerType == TriggerType.Cron && string.IsNullOrWhiteSpace(cronExpression))
            throw new ArgumentException("CronExpression is required for Cron trigger type.", nameof(cronExpression));

        TriggerType = triggerType;
        WebhookSecret = triggerType == TriggerType.Webhook ? webhookSecret : null;
        CronExpression = triggerType == TriggerType.Cron ? cronExpression : null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the name and description of the workflow.
    /// </summary>
    /// <param name="name">The new name of the workflow.</param>
    /// <param name="description">The new description of the workflow.</param>
    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the retry policy for node executions within this workflow.
    /// </summary>
    /// <param name="policy">The retry policy to apply.</param>
    public void SetRetryPolicy(RetryPolicy policy)
    {
        RetryPolicy = policy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the workflow as deleted.
    /// </summary>
    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restores the workflow to an active state.
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}