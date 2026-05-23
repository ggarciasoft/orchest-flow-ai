using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrchestAI.Infrastructure.Queue;
using OrchestAI.Engine;
namespace OrchestAI.Worker.Workers;

public sealed class ExecutionWorker : BackgroundService
{
    private readonly InMemoryExecutionQueue _queue;
    private readonly IWorkflowEngine _engine;
    private readonly ILogger<ExecutionWorker> _logger;

    public ExecutionWorker(InMemoryExecutionQueue queue, IWorkflowEngine engine, ILogger<ExecutionWorker> logger)
    { _queue = queue; _engine = engine; _logger = logger; }

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
