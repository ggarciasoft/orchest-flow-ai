using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Engine;

namespace OrchestFlowAI.Worker.Workers;

/// <summary>Background worker that dequeues and resumes paused workflow executions (e.g. after approvals).</summary>
public sealed class ResumeWorker : BackgroundService
{
    private readonly IExecutionQueueConsumer _queue;
    private readonly IWorkflowEngine _engine;
    private readonly ILogger<ResumeWorker> _logger;

    /// <summary>Initialises the worker with a queue consumer and engine.</summary>
    public ResumeWorker(IExecutionQueueConsumer queue, IWorkflowEngine engine, ILogger<ResumeWorker> logger)
    { _queue = queue; _engine = engine; _logger = logger; }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ResumeWorker started");
        await foreach (var msg in _queue.ReadAllResumeAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Resuming execution {ExecutionId}", msg.ExecutionId);
                var signal = new ResumeSignal(msg.NodeExecutionId, msg.ResumeOutputs);
                await _engine.ResumeAsync(msg.ExecutionId, signal, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error resuming execution {ExecutionId}", msg.ExecutionId);
            }
        }
    }
}
