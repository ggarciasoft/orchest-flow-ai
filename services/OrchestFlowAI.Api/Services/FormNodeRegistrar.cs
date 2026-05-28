using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Engine.Registry;
using OrchestFlowAI.Nodes.Human;

namespace OrchestFlowAI.Api.Services;

/// <summary>
/// Loads and registers all custom form nodes at API startup.
/// RefreshAsync is called by FormsController after every create/update/delete
/// to keep the registry and node catalog in sync immediately.
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
            {
                var activeVersion = await repo.GetActiveVersionAsync(form.Id, ct);
                Register(form, activeVersion?.VersionNumber);
            }
            _logger.LogInformation("FormNodeRegistrar: registered {Count} form nodes.", forms.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FormNodeRegistrar: failed to load form nodes at startup.");
        }
    }

    /// <summary>
    /// Full refresh for a tenant: unregisters deleted form nodes, registers new/updated ones.
    /// Called by FormsController after any create/update/delete.
    /// </summary>
    public async Task RefreshAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFormRepository>();
        var forms = await repo.ListAllAsync(ct);

        // Determine which form types should exist for this tenant
        var tenantForms = forms.Where(f => f.TenantId == tenantId).ToList();
        var expectedTypes = tenantForms.Select(f => $"form.{f.Slug}").ToHashSet();

        // Unregister stale form types for this tenant
        var stale = _registry.GetAllDescriptors()
            .Where(d => d.Type.StartsWith("form.", StringComparison.OrdinalIgnoreCase)
                     && !expectedTypes.Contains(d.Type))
            .ToList();

        foreach (var d in stale)
        {
            _registry.Unregister(d.Type);
            _logger.LogInformation("FormNodeRegistrar: unregistered stale form node {Type}", d.Type);
        }

        // Register / re-register current forms with their active version number
        foreach (var form in tenantForms)
        {
            var activeVersion = await repo.GetActiveVersionAsync(form.Id, ct);
            Register(form, activeVersion?.VersionNumber);
        }
    }

    private void Register(OrchestFlowAI.Domain.Entities.Form form, int? activeVersionNumber = null)
    {
        _registry.Register(new DynamicFormNode(form, activeVersionNumber), new DynamicFormNodeDescriptor(form));
    }
}
