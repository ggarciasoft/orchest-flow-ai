using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Engine.Registry;
using OrchestFlowAI.Nodes.Human;

namespace OrchestFlowAI.Worker.Services;

/// <summary>
/// Loads and hot-reloads custom form nodes into the engine registry.
///
/// On startup: loads all forms from the DB and registers them as form.{slug} node types.
/// Polling loop: every <see cref="RefreshIntervalSeconds"/> seconds, re-loads all forms,
/// unregisters removed/changed entries, and registers new/updated ones.
/// This means a new form created while the worker is running will be available within
/// one polling interval — no worker restart required.
/// </summary>
public sealed class WorkerFormNodeRegistrar : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INodeRegistry _registry;
    private readonly ILogger<WorkerFormNodeRegistrar> _logger;

    /// <summary>How often the registry refreshes from the database (default 30 s).</summary>
    public static int RefreshIntervalSeconds { get; set; } = 30;

    public WorkerFormNodeRegistrar(
        IServiceScopeFactory scopeFactory,
        INodeRegistry registry,
        ILogger<WorkerFormNodeRegistrar> logger)
    {
        _scopeFactory = scopeFactory;
        _registry = registry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial load — run before ExecutionWorker starts dequeuing jobs
        await RefreshAsync(stoppingToken);

        // Polling loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(RefreshIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RefreshAsync(stoppingToken);
        }
    }

    public async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IFormRepository>();
            var forms = await repo.ListAllAsync(ct);

            // Build a set of currently known form types from the DB
            var dbTypes = new HashSet<string>(forms.Select(f => $"form.{f.Slug}"));

            // Unregister any form nodes that no longer exist in the DB
            var existingFormDescriptors = _registry.GetAllDescriptors()
                .Where(d => d.Type.StartsWith("form.", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var descriptor in existingFormDescriptors)
            {
                if (!dbTypes.Contains(descriptor.Type))
                {
                    _registry.Unregister(descriptor.Type);
                    _logger.LogInformation("WorkerFormNodeRegistrar: unregistered removed form node {Type}", descriptor.Type);
                }
            }

            // Register / re-register all current forms (Register overwrites in ConcurrentDictionary)
            foreach (var form in forms)
            {
                var activeVersion = await repo.GetActiveVersionAsync(form.Id, ct);
                _registry.Register(new DynamicFormNode(form, activeVersion?.VersionNumber), new DynamicFormNodeDescriptor(form));
            }

            _logger.LogDebug("WorkerFormNodeRegistrar: refreshed {Count} form nodes.", forms.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WorkerFormNodeRegistrar: failed to refresh form nodes.");
        }
    }
}
