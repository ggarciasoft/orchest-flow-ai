namespace OrchestAI.Contracts;

/// <summary>Defines how a workflow execution is triggered. Mirrors <c>OrchestAI.Domain.Enums.TriggerType</c>.</summary>
public enum TriggerType
{
    /// <summary>The workflow is triggered manually by a user.</summary>
    Manual = 0,

    /// <summary>The workflow is triggered by an inbound HTTP webhook request.</summary>
    Webhook = 1,

    /// <summary>The workflow is triggered on a recurring cron schedule.</summary>
    Cron = 2,
}
