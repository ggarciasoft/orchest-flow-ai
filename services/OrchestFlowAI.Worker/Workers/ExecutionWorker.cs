using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Engine;

namespace OrchestFlowAI.Worker.Workers;

/// <summary>Background worker that dequeues and runs workflow executions.</summary>
public sealed class ExecutionWorker : BackgroundService
{
    private readonly IExecutionQueueConsumer _queue;
    private readonly IWorkflowEngine _engine;
    private readonly ILogger<ExecutionWorker> _logger;

    /// <summary>Initialises the worker with a queue consumer and engine.</summary>
    public ExecutionWorker(IExecutionQueueConsumer queue, IWorkflowEngine engine, ILogger<ExecutionWorker> logger)
    { _queue = queue; _engine = engine; _logger = logger; }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExecutionWorker started");
        await foreach (var msg in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing execution {ExecutionId}", msg.ExecutionId);
                await _engine.RunAsync(msg.ExecutionId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing execution {ExecutionId}", msg.ExecutionId);
            }
        }
    }
}
