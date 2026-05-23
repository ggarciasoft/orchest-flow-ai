using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrchestAI.Infrastructure.Queue;
using OrchestAI.Engine;
namespace OrchestAI.Worker.Workers;

public sealed class ResumeWorker : BackgroundService
{
    private readonly InMemoryExecutionQueue _queue;
    private readonly IWorkflowEngine _engine;
    private readonly ILogger<ResumeWorker> _logger;

    public ResumeWorker(InMemoryExecutionQueue queue, IWorkflowEngine engine, ILogger<ResumeWorker> logger)
    { _queue = queue; _engine = engine; _logger = logger; }

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
