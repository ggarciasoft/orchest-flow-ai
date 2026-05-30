using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Api.Services;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Engine.Registry;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Tests.ServiceTests;

public sealed class FormNodeRegistrarTests
{
    // ─── helpers ────────────────────────────────────────────────────────────

    private static Form MakeForm(Guid tenantId, string slug) =>
        Form.Create(tenantId, slug, slug, null, "[]");

    private static IWorkflowNodeDescriptor MakeDescriptor(string type)
    {
        var mock = new Mock<IWorkflowNodeDescriptor>();
        mock.Setup(d => d.Type).Returns(type);
        mock.Setup(d => d.DisplayName).Returns(type);
        mock.Setup(d => d.Description).Returns(string.Empty);
        mock.Setup(d => d.Category).Returns("Forms");
        mock.Setup(d => d.Version).Returns("1.0");
        mock.Setup(d => d.IconKey).Returns((string?)null);
        mock.Setup(d => d.Inputs).Returns(Array.Empty<NodeInputDefinition>());
        mock.Setup(d => d.Outputs).Returns(Array.Empty<NodeOutputDefinition>());
        mock.Setup(d => d.Configuration).Returns(Array.Empty<NodeConfigDefinition>());
        return mock.Object;
    }

    private static (FormNodeRegistrar registrar, Mock<INodeRegistry> registry, Mock<IFormRepository> repo)
        Build(IReadOnlyList<Form> allForms, IReadOnlyCollection<IWorkflowNodeDescriptor> registeredDescriptors)
    {
        var repo = new Mock<IFormRepository>();
        repo.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allForms);
        repo.Setup(r => r.GetActiveVersionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FormVersion?)null);

        var registry = new Mock<INodeRegistry>();
        registry.Setup(r => r.GetAllDescriptors()).Returns(registeredDescriptors);

        var services = new ServiceCollection();
        services.AddScoped<IFormRepository>(_ => repo.Object);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        var registrar = new FormNodeRegistrar(scopeFactory, registry.Object, NullLogger<FormNodeRegistrar>.Instance);
        return (registrar, registry, repo);
    }

    // ─── tests ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_ShouldUnregister_StaleFormNodeForTenant()
    {
        var tenantA = Guid.NewGuid();
        // tenantA now only has "alpha"; "old-form" was deleted
        var allForms = new List<Form> { MakeForm(tenantA, "alpha") };
        var descriptors = new List<IWorkflowNodeDescriptor>
        {
            MakeDescriptor("form.alpha"),
            MakeDescriptor("form.old-form"),  // stale for tenantA
        };

        var (registrar, registry, _) = Build(allForms, descriptors);

        await registrar.RefreshAsync(tenantA);

        registry.Verify(r => r.Unregister("form.old-form"), Times.Once);
        registry.Verify(r => r.Unregister("form.alpha"), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_ShouldNotUnregister_FormNodesFromOtherTenants()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // tenantA deletes its only form; tenantB still has "beta"
        var allForms = new List<Form> { MakeForm(tenantB, "beta") };
        var descriptors = new List<IWorkflowNodeDescriptor>
        {
            MakeDescriptor("form.alpha"),  // previously tenantA's, now deleted from DB
            MakeDescriptor("form.beta"),   // tenantB's — must not be touched
        };

        var (registrar, registry, _) = Build(allForms, descriptors);

        await registrar.RefreshAsync(tenantA);

        // form.beta belongs to tenantB — must never be unregistered
        registry.Verify(r => r.Unregister("form.beta"), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_ShouldUnregister_DeletedTenantFormAndLeaveOtherTenantIntact()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // tenantA: "alpha" remains, "gone" was deleted
        // tenantB: "beta" remains
        var allForms = new List<Form>
        {
            MakeForm(tenantA, "alpha"),
            MakeForm(tenantB, "beta"),
        };
        var descriptors = new List<IWorkflowNodeDescriptor>
        {
            MakeDescriptor("form.alpha"),
            MakeDescriptor("form.gone"),   // stale for tenantA
            MakeDescriptor("form.beta"),   // tenantB — untouchable
        };

        var (registrar, registry, _) = Build(allForms, descriptors);

        await registrar.RefreshAsync(tenantA);

        registry.Verify(r => r.Unregister("form.gone"), Times.Once);
        registry.Verify(r => r.Unregister("form.alpha"), Times.Never);
        registry.Verify(r => r.Unregister("form.beta"), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_ShouldRegister_AllCurrentTenantForms()
    {
        var tenantA = Guid.NewGuid();
        var allForms = new List<Form>
        {
            MakeForm(tenantA, "alpha"),
            MakeForm(tenantA, "bravo"),
        };
        var descriptors = new List<IWorkflowNodeDescriptor>();

        var (registrar, registry, _) = Build(allForms, descriptors);

        await registrar.RefreshAsync(tenantA);

        registry.Verify(r => r.Register(It.IsAny<IWorkflowNode>(), It.IsAny<IWorkflowNodeDescriptor>()), Times.Exactly(2));
    }
}
