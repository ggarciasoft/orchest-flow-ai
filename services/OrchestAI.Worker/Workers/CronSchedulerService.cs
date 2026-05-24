using Cronos;
using OrchestAI.Application.Abstractions;
using OrchestAI.Contracts.Events;
using OrchestAI.Domain.Entities;
using OrchestAI.Domain.Enums;

namespace OrchestAI.Worker.Workers;

/// <summary>
/// Background service that evaluates cron-triggered workflows every 60 seconds and enqueues
/// executions when the scheduled time has elapsed since the last trigger.
/// </summary>
public sealed class CronSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CronSchedulerService> _logger;
    private readonly TimeProvider _timeProvider;

    // In-memory tracking: workflowId -> last time it was triggered this minute
    private readonly Dictionary<Guid, DateTime> _lastTriggered = new();

    /// <summary>
    /// Initializes a new instance of <see cref="CronSchedulerService"/>.
    /// </summary>
    /// <param name="scopeFactory">Factory used to create scoped DI containers for each evaluation cycle.</param>
    /// <param name="logger">Structured logger.</param>
    /// <param name="timeProvider">Abstracted clock — use <see cref="TimeProvider.System"/> in production; inject a fake in tests.</param>
    public CronSchedulerService(
        IServiceScopeFactory scopeFactory,
        ILogger<CronSchedulerService> logger,
        TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CronSchedulerService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await EvaluateCronWorkflowsAsync(stoppingToken);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("CronSchedulerService stopped.");
    }

    /// <summary>
    /// Queries all cron-triggered workflows and enqueues executions for those whose next
    /// scheduled occurrence has passed and was not already triggered within this minute.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    internal async Task EvaluateCronWorkflowsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var workflows = scope.ServiceProvider.GetRequiredService<IWorkflowRepository>();
        var executions = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
        var queue = scope.ServiceProvider.GetRequiredService<IExecutionQueue>();

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var cronWorkflows = await workflows.ListByTriggerTypeAsync(TriggerType.Cron, ct);

        foreach (var workflow in cronWorkflows)
        {
            if (string.IsNullOrWhiteSpace(workflow.CronExpression))
                continue;

            CronExpression cron;
            try
            {
                cron = CronExpression.Parse(workflow.CronExpression, CronFormat.Standard);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Invalid cron expression '{Expr}' for workflow {WorkflowId}. Skipping.",
                    workflow.CronExpression, workflow.Id);
                continue;
            }

            // Check whether this workflow was already triggered within the current minute window
            if (_lastTriggered.TryGetValue(workflow.Id, out var lastTrigger))
            {
                var minuteStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);
                if (lastTrigger >= minuteStart)
                    continue;
            }

            // Look 61 seconds back so we catch the most recent occurrence up to now
            var from = now.AddSeconds(-61);
            var next = cron.GetNextOccurrence(from, TimeZoneInfo.Utc);

            if (next == null || next > now)
                continue;

            // This cron is due — enqueue execution
            var activeVersion = await workflows.GetActiveVersionAsync(workflow.Id, ct);
            if (activeVersion == null)
            {
                _logger.LogWarning("Cron workflow {WorkflowId} has no active version. Skipping.", workflow.Id);
                continue;
            }

            var correlationId = Guid.NewGuid().ToString();
            var execution = Domain.Entities.WorkflowExecution.Create(
                workflow.TenantId,
                workflow.Id,
                activeVersion.Id,
                triggeredBy: null,
                inputJson: "{}",
                correlationId: correlationId);

            await executions.CreateAsync(execution, ct);
            await queue.EnqueueAsync(new ExecutionQueueMessage(execution.Id, workflow.TenantId, correlationId), ct);

            _lastTriggered[workflow.Id] = now;

            _logger.LogInformation(
                "Cron-triggered workflow {WorkflowId} enqueued execution {ExecutionId}.",
                workflow.Id, execution.Id);
        }
    }
}
