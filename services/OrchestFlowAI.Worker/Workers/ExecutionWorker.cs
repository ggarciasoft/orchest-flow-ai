using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Engine;

namespace OrchestFlowAI.Worker.Workers;

/// <summary>Background worker that dequeues and runs workflow executions.</summary>
public sealed class ExecutionWorker : BackgroundService
{
    private readonly IExecutionQueueConsumer _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExecutionWorker> _logger;

    /// <summary>Initialises the worker with a queue consumer, scope factory, and logger.</summary>
    public ExecutionWorker(IExecutionQueueConsumer queue, IServiceScopeFactory scopeFactory, ILogger<ExecutionWorker> logger)
    { _queue = queue; _scopeFactory = scopeFactory; _logger = logger; }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExecutionWorker started");
        await foreach (var msg in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing execution {ExecutionId}", msg.ExecutionId);
                // Create a fresh scope per execution so each gets its own DbContext instance
                using var scope = _scopeFactory.CreateScope();
                var engine = scope.ServiceProvider.GetRequiredService<IWorkflowEngine>();

                // Enforce execution timeout from tenant config (0 = unlimited)
                var execRepo = scope.ServiceProvider.GetRequiredService<OrchestFlowAI.Application.Abstractions.IExecutionRepository>();
                var execution = await execRepo.GetAsync(msg.ExecutionId, stoppingToken);
                var timeoutSeconds = execution?.TimeoutSeconds ?? 0;

                if (timeoutSeconds > 0)
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                    await engine.RunAsync(msg.ExecutionId, cts.Token);
                }
                else
                {
                    await engine.RunAsync(msg.ExecutionId, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing execution {ExecutionId}", msg.ExecutionId);
            }
        }
    }
}
