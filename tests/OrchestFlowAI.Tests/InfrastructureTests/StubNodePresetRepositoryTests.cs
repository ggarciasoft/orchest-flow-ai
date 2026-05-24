using FluentAssertions;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Infrastructure.Repositories;

namespace OrchestFlowAI.Tests.InfrastructureTests;

/// <summary>
/// Tests for <see cref="StubNodePresetRepository"/> — the in-memory preset store
/// used when no database is configured.
/// </summary>
public sealed class StubNodePresetRepositoryTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    private static StubNodePresetRepository Fresh() => new();

    // ──────────────────────────────────────────────────────────────────────────
    // Create + Get
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ShouldPersistPreset()
    {
        var repo = Fresh();
        var preset = NodePreset.Create(TenantA, "Bearer Default", "integrations.http", @"{""authType"":""bearer""}");

        var created = await repo.CreateAsync(preset);

        created.Should().BeSameAs(preset);
    }

    [Fact]
    public async Task GetAsync_ExistingPreset_ShouldReturnIt()
    {
        var repo = Fresh();
        var preset = NodePreset.Create(TenantA, "P", "integrations.http", "{}");
        await repo.CreateAsync(preset);

        var found = await repo.GetAsync(preset.Id, TenantA);

        found.Should().NotBeNull();
        found!.Id.Should().Be(preset.Id);
        found.Name.Should().Be("P");
    }

    [Fact]
    public async Task GetAsync_WrongTenantId_ShouldReturnNull()
    {
        var repo = Fresh();
        var preset = NodePreset.Create(TenantA, "P", "integrations.http", "{}");
        await repo.CreateAsync(preset);

        var found = await repo.GetAsync(preset.Id, TenantB);

        found.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_UnknownId_ShouldReturnNull()
    {
        var repo = Fresh();
        var found = await repo.GetAsync(Guid.NewGuid(), TenantA);
        found.Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ListByNodeType
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListByNodeTypeAsync_NoFilter_ShouldReturnAllForTenant()
    {
        var repo = Fresh();
        await repo.CreateAsync(NodePreset.Create(TenantA, "A", "integrations.http", "{}"));
        await repo.CreateAsync(NodePreset.Create(TenantA, "B", "logic.switch", "{}"));
        await repo.CreateAsync(NodePreset.Create(TenantB, "C", "integrations.http", "{}"));

        var list = await repo.ListByNodeTypeAsync(TenantA, null);

        list.Should().HaveCount(2);
        list.Select(p => p.Name).Should().BeEquivalentTo(new[] { "A", "B" });
    }

    [Fact]
    public async Task ListByNodeTypeAsync_WithNodeTypeFilter_ShouldReturnMatchingOnly()
    {
        var repo = Fresh();
        await repo.CreateAsync(NodePreset.Create(TenantA, "HTTP1", "integrations.http", "{}"));
        await repo.CreateAsync(NodePreset.Create(TenantA, "HTTP2", "integrations.http", "{}"));
        await repo.CreateAsync(NodePreset.Create(TenantA, "Slack1", "integrations.slack", "{}"));

        var list = await repo.ListByNodeTypeAsync(TenantA, "integrations.http");

        list.Should().HaveCount(2);
        list.Should().OnlyContain(p => p.NodeType == "integrations.http");
    }

    [Fact]
    public async Task ListByNodeTypeAsync_NoMatchingNodeType_ShouldReturnEmpty()
    {
        var repo = Fresh();
        await repo.CreateAsync(NodePreset.Create(TenantA, "P", "integrations.http", "{}"));

        var list = await repo.ListByNodeTypeAsync(TenantA, "logic.delay");

        list.Should().BeEmpty();
    }

    [Fact]
    public async Task ListByNodeTypeAsync_ShouldReturnResultsOrderedByName()
    {
        var repo = Fresh();
        await repo.CreateAsync(NodePreset.Create(TenantA, "Zebra", "integrations.http", "{}"));
        await repo.CreateAsync(NodePreset.Create(TenantA, "Apple", "integrations.http", "{}"));
        await repo.CreateAsync(NodePreset.Create(TenantA, "Mango", "integrations.http", "{}"));

        var list = await repo.ListByNodeTypeAsync(TenantA, "integrations.http");

        list.Select(p => p.Name).Should().ContainInOrder("Apple", "Mango", "Zebra");
    }

    [Fact]
    public async Task ListByNodeTypeAsync_TenantIsolation_ShouldNotLeakCrossTenant()
    {
        var repo = Fresh();
        await repo.CreateAsync(NodePreset.Create(TenantA, "A-Preset", "integrations.http", "{}"));
        await repo.CreateAsync(NodePreset.Create(TenantB, "B-Preset", "integrations.http", "{}"));

        var listA = await repo.ListByNodeTypeAsync(TenantA, null);
        var listB = await repo.ListByNodeTypeAsync(TenantB, null);

        listA.Should().HaveCount(1).And.OnlyContain(p => p.TenantId == TenantA);
        listB.Should().HaveCount(1).And.OnlyContain(p => p.TenantId == TenantB);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Update
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        var repo = Fresh();
        var preset = NodePreset.Create(TenantA, "Old", "integrations.http", "{}");
        await repo.CreateAsync(preset);

        preset.Update("New", @"{""url"":""https://updated.example.com""}");
        await repo.UpdateAsync(preset);

        var found = await repo.GetAsync(preset.Id, TenantA);
        found!.Name.Should().Be("New");
        found.ConfigJson.Should().Contain("updated.example.com");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Delete
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ShouldRemovePreset()
    {
        var repo = Fresh();
        var preset = NodePreset.Create(TenantA, "P", "integrations.http", "{}");
        await repo.CreateAsync(preset);

        await repo.DeleteAsync(preset.Id, TenantA);

        var found = await repo.GetAsync(preset.Id, TenantA);
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ShouldNotThrow()
    {
        var repo = Fresh();
        var act = async () => await repo.DeleteAsync(Guid.NewGuid(), TenantA);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_ShouldOnlyRemoveTargetPreset()
    {
        var repo = Fresh();
        var p1 = NodePreset.Create(TenantA, "Keep", "integrations.http", "{}");
        var p2 = NodePreset.Create(TenantA, "Remove", "integrations.http", "{}");
        await repo.CreateAsync(p1);
        await repo.CreateAsync(p2);

        await repo.DeleteAsync(p2.Id, TenantA);

        var list = await repo.ListByNodeTypeAsync(TenantA, null);
        list.Should().HaveCount(1);
        list[0].Name.Should().Be("Keep");
    }
}
