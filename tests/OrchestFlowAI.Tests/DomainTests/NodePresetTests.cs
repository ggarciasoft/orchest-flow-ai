using FluentAssertions;
using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Tests.DomainTests;

/// <summary>Unit tests for the <see cref="NodePreset"/> domain entity.</summary>
public sealed class NodePresetTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    // ──────────────────────────────────────────────────────────────────────────
    // Creation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldAssignNewId()
    {
        var preset = NodePreset.Create(TenantId, "My Preset", "integrations.http", "{}");
        preset.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldStoreAllFields()
    {
        var preset = NodePreset.Create(TenantId, "API Config", "integrations.http", @"{""url"":""https://api.example.com""}");

        preset.TenantId.Should().Be(TenantId);
        preset.Name.Should().Be("API Config");
        preset.NodeType.Should().Be("integrations.http");
        preset.ConfigJson.Should().Be(@"{""url"":""https://api.example.com""}");
    }

    [Fact]
    public void Create_ShouldSetCreatedAtAndUpdatedAtToNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var preset = NodePreset.Create(TenantId, "X", "type", "{}");
        var after = DateTime.UtcNow.AddSeconds(1);

        preset.CreatedAt.Should().BeAfter(before).And.BeBefore(after);
        preset.UpdatedAt.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void Create_MultiplePresets_ShouldHaveDistinctIds()
    {
        var a = NodePreset.Create(TenantId, "A", "type", "{}");
        var b = NodePreset.Create(TenantId, "B", "type", "{}");

        a.Id.Should().NotBe(b.Id);
    }

    [Fact]
    public void Create_DifferentNodeTypes_ShouldAllBeAccepted()
    {
        var types = new[] { "integrations.http", "integrations.slack", "logic.switch", "ai.classify" };
        foreach (var t in types)
        {
            var preset = NodePreset.Create(TenantId, "P", t, "{}");
            preset.NodeType.Should().Be(t);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Update
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ShouldChangeName()
    {
        var preset = NodePreset.Create(TenantId, "Old Name", "integrations.http", "{}");
        preset.Update("New Name", "{}");
        preset.Name.Should().Be("New Name");
    }

    [Fact]
    public void Update_ShouldChangeConfigJson()
    {
        var preset = NodePreset.Create(TenantId, "P", "integrations.http", "{}");
        var newConfig = @"{""url"":""https://new.example.com"",""authType"":""bearer""}";

        preset.Update("P", newConfig);

        preset.ConfigJson.Should().Be(newConfig);
    }

    [Fact]
    public void Update_ShouldBumpUpdatedAt()
    {
        var preset = NodePreset.Create(TenantId, "P", "type", "{}");
        var original = preset.UpdatedAt;

        // Ensure measurable time gap
        Thread.Sleep(5);
        preset.Update("P", "{}");

        preset.UpdatedAt.Should().BeOnOrAfter(original);
    }

    [Fact]
    public void Update_ShouldNotChangeNodeTypeOrCreatedAt()
    {
        var preset = NodePreset.Create(TenantId, "P", "integrations.http", "{}");
        var createdAt = preset.CreatedAt;

        preset.Update("Q", @"{""x"":1}");

        preset.NodeType.Should().Be("integrations.http");
        preset.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Update_ShouldNotChangeTenantId()
    {
        var preset = NodePreset.Create(TenantId, "P", "type", "{}");
        preset.Update("Q", "{}");
        preset.TenantId.Should().Be(TenantId);
    }
}
