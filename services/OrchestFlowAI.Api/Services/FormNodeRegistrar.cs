using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Engine.Registry;
using OrchestFlowAI.Nodes.Human;

namespace OrchestFlowAI.Api.Services;

/// <summary>
/// Loads and registers all custom form nodes at startup and provides an on-demand refresh
/// method called by <see cref="Controllers.FormsController"/> after create/update/delete.
/// </summary>
public sealed class FormNodeRegistrar : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INodeRegistry _registry;
    private readonly ILogger<FormNodeRegistrar> _logger;

    public FormNodeRegistrar(IServiceScopeFactory scopeFactory, INodeRegistry registry, ILogger<FormNodeRegistrar> logger)
    {
        _scopeFactory = scopeFactory;
        _registry = registry;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await LoadAllAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task LoadAllAsync(CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IFormRepository>();
            var forms = await repo.ListAllAsync(ct);
            foreach (var form in forms)
                Register(form);
            _logger.LogInformation("FormNodeRegistrar: registered {Count} form nodes.", forms.Count);
        }
        catch (Exception ex)
        {
            // Non-fatal: forms may not have been seeded yet (e.g. no DB at startup)
            _logger.LogWarning(ex, "FormNodeRegistrar: failed to load form nodes at startup.");
        }
    }

    public async Task RefreshAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFormRepository>();
        var forms = await repo.ListAsync(tenantId, ct);
        foreach (var form in forms)
            Register(form);
    }

    private void Register(OrchestFlowAI.Domain.Entities.Form form)
    {
        var node = new DynamicFormNode(form);
        var descriptor = new DynamicFormNodeDescriptor(form);
        _registry.Register(node, descriptor);
    }
}
