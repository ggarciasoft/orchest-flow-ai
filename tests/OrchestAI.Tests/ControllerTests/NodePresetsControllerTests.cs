using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrchestAI.Api.Controllers;
using OrchestAI.Application.Abstractions;
using OrchestAI.Domain.Entities;

namespace OrchestAI.Tests.ControllerTests;

/// <summary>
/// Unit tests for <see cref="NodePresetsController"/> — exercises CRUD endpoints
/// against a mocked <see cref="INodePresetRepository"/>.
/// </summary>
public sealed class NodePresetsControllerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PresetId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static NodePreset MakePreset(string name = "Default HTTP", string type = "integrations.http", string config = @"{""url"":""https://api.example.com""}")
    {
        var p = NodePreset.Create(TenantId, name, type, config);
        return p;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/presets
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListPresets_ReturnsOk_WithPresetDtos()
    {
        var preset = MakePreset();
        var repo = new Mock<INodePresetRepository>();
        repo.Setup(r => r.ListByNodeTypeAsync(TenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { preset });
        var controller = new NodePresetsController(repo.Object);

        var result = await controller.ListPresets(TenantId, null, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<PresetResponse>>().Subject.ToList();
        list.Should().HaveCount(1);
        list[0].Name.Should().Be("Default HTTP");
        list[0].NodeType.Should().Be("integrations.http");
    }

    [Fact]
    public async Task ListPresets_WithNodeTypeFilter_PassesFilterToRepository()
    {
        var repo = new Mock<INodePresetRepository>();
        repo.Setup(r => r.ListByNodeTypeAsync(TenantId, "integrations.http", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<NodePreset>());
        var controller = new NodePresetsController(repo.Object);

        await controller.ListPresets(TenantId, "integrations.http", CancellationToken.None);

        repo.Verify(r => r.ListByNodeTypeAsync(TenantId, "integrations.http", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListPresets_EmptyRepository_ReturnsOkWithEmptyList()
    {
        var repo = new Mock<INodePresetRepository>();
        repo.Setup(r => r.ListByNodeTypeAsync(TenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<NodePreset>());
        var controller = new NodePresetsController(repo.Object);

        var result = await controller.ListPresets(TenantId, null, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<PresetResponse>>().Subject
            .Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/presets/{id}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPreset_ExistingId_ReturnsOkWithDto()
    {
        var preset = MakePreset();
        var repo = new Mock<INodePresetRepository>();
        repo.Setup(r => r.GetAsync(preset.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preset);
        var controller = new NodePresetsController(repo.Object);

        var result = await controller.GetPreset(preset.Id, TenantId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PresetResponse>().Subject;
        dto.Id.Should().Be(preset.Id);
        dto.Name.Should().Be(preset.Name);
    }

    [Fact]
    public async Task GetPreset_UnknownId_ReturnsNotFound()
    {
        var repo = new Mock<INodePresetRepository>();
        repo.Setup(r => r.GetAsync(It.IsAny<Guid>(), TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodePreset?)null);
        var controller = new NodePresetsController(repo.Object);

        var result = await controller.GetPreset(Guid.NewGuid(), TenantId, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/presets
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePreset_ValidRequest_ReturnsCreatedAtAction()
    {
        var repo = new Mock<INodePresetRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<NodePreset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodePreset p, CancellationToken _) => p);
        var controller = new NodePresetsController(repo.Object);

        var request = new CreatePresetRequest("Bearer Auth", "integrations.http", @"{""authType"":""bearer""}");
        var result = await controller.CreatePreset(request, TenantId, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(NodePresetsController.GetPreset));
        var dto = created.Value.Should().BeOfType<PresetResponse>().Subject;
        dto.Name.Should().Be("Bearer Auth");
        dto.NodeType.Should().Be("integrations.http");
        dto.ConfigJson.Should().Contain("bearer");
    }

    [Fact]
    public async Task CreatePreset_ShouldCallRepositoryCreate()
    {
        var repo = new Mock<INodePresetRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<NodePreset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodePreset p, CancellationToken _) => p);
        var controller = new NodePresetsController(repo.Object);

        await controller.CreatePreset(new("My Preset", "integrations.http", "{}"), TenantId, CancellationToken.None);

        repo.Verify(r => r.CreateAsync(
            It.Is<NodePreset>(p => p.Name == "My Preset" && p.TenantId == TenantId),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PUT /api/presets/{id}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePreset_ExistingId_ReturnsOkWithUpdatedDto()
    {
        var preset = MakePreset("Old Name");
        var repo = new Mock<INodePresetRepository>();
        repo.Setup(r => r.GetAsync(preset.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preset);
        repo.Setup(r => r.UpdateAsync(preset, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = new NodePresetsController(repo.Object);

        var result = await controller.UpdatePreset(
            preset.Id, TenantId,
            new UpdatePresetRequest("New Name", @"{""authType"":""basic""}"),
            CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PresetResponse>().Subject;
        dto.Name.Should().Be("New Name");
        dto.ConfigJson.Should().Contain("basic");
    }

    [Fact]
    public async Task UpdatePreset_UnknownId_ReturnsNotFound()
    {
        var repo = new Mock<INodePresetRepository>();
        repo.Setup(r => r.GetAsync(It.IsAny<Guid>(), TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodePreset?)null);
        var controller = new NodePresetsController(repo.Object);

        var result = await controller.UpdatePreset(
            Guid.NewGuid(), TenantId,
            new UpdatePresetRequest("X", "{}"),
            CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdatePreset_ShouldCallRepositoryUpdate()
    {
        var preset = MakePreset();
        var repo = new Mock<INodePresetRepository>();
        repo.Setup(r => r.GetAsync(preset.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preset);
        repo.Setup(r => r.UpdateAsync(preset, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = new NodePresetsController(repo.Object);

        await controller.UpdatePreset(preset.Id, TenantId, new("Updated", "{}"), CancellationToken.None);

        repo.Verify(r => r.UpdateAsync(preset, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DELETE /api/presets/{id}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePreset_ShouldReturnNoContent()
    {
        var repo = new Mock<INodePresetRepository>();
        repo.Setup(r => r.DeleteAsync(PresetId, TenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = new NodePresetsController(repo.Object);

        var result = await controller.DeletePreset(PresetId, TenantId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeletePreset_ShouldCallRepositoryDelete()
    {
        var repo = new Mock<INodePresetRepository>();
        repo.Setup(r => r.DeleteAsync(PresetId, TenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = new NodePresetsController(repo.Object);

        await controller.DeletePreset(PresetId, TenantId, CancellationToken.None);

        repo.Verify(r => r.DeleteAsync(PresetId, TenantId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
