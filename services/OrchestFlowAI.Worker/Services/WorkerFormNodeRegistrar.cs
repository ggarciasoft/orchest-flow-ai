using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Engine.Registry;
using OrchestFlowAI.Nodes.Human;

namespace OrchestFlowAI.Worker.Services;

/// <summary>
/// Loads and registers all custom form nodes into the engine registry at worker startup.
/// Mirrors the same service in OrchestFlowAI.Api so that the execution worker can resolve
/// form.{slug} node types when running workflows.
/// </summary>
public sealed class WorkerFormNodeRegistrar : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INodeRegistry _registry;
    private readonly ILogger<WorkerFormNodeRegistrar> _logger;

    public WorkerFormNodeRegistrar(
        IServiceScopeFactory scopeFactory,
        INodeRegistry registry,
        ILogger<WorkerFormNodeRegistrar> logger)
    {
        _scopeFactory = scopeFactory;
        _registry = registry;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IFormRepository>();
            var forms = await repo.ListAllAsync(cancellationToken);
            foreach (var form in forms)
            {
                _registry.Register(new DynamicFormNode(form), new DynamicFormNodeDescriptor(form));
            }
            _logger.LogInformation("WorkerFormNodeRegistrar: registered {Count} form nodes.", forms.Count);
        }
        catch (Exception ex)
        {
            // Non-fatal at startup (DB may not be ready yet), but log so it's visible
            _logger.LogWarning(ex, "WorkerFormNodeRegistrar: failed to load form nodes at startup.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
