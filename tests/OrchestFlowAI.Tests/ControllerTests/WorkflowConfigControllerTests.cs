using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrchestFlowAI.Api.Controllers;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Tests.ControllerTests;

/// <summary>
/// Unit tests for <see cref="WorkflowConfigController"/> — exercises list, get, create, update, and delete
/// against mocked <see cref="IWorkflowConfigRepository"/>.
/// </summary>
public sealed class WorkflowConfigControllerTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static WorkflowConfigController BuildController(IWorkflowConfigRepository repo)
    {
        var controller = new WorkflowConfigController(repo);
        var claims = new[] { new Claim("tenant_id", TenantId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        return controller;
    }

    private static WorkflowConfig MakeConfig(string key = "test-key", string value = "test-value")
        => WorkflowConfig.Create(TenantId, key, value, "string", "Test description");

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/config
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_ReturnsOk_WithConfigEntries()
    {
        var config = MakeConfig();
        var repo = new Mock<IWorkflowConfigRepository>();
        repo.Setup(r => r.ListAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { config });
        var controller = BuildController(repo.Object);

        var result = await controller.List(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value as IEnumerable<object>;
        list.Should().NotBeNull().And.HaveCount(1);
    }

    [Fact]
    public async Task List_EmptyTenant_ReturnsOkWithEmptyList()
    {
        var repo = new Mock<IWorkflowConfigRepository>();
        repo.Setup(r => r.ListAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<WorkflowConfig>());
        var controller = BuildController(repo.Object);

        var result = await controller.List(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value as IEnumerable<object>;
        list.Should().NotBeNull().And.BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/config/{key}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ExistingKey_ReturnsOkWithConfig()
    {
        var config = MakeConfig("my-key", "my-value");
        var repo = new Mock<IWorkflowConfigRepository>();
        repo.Setup(r => r.GetAsync(TenantId, "my-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        var controller = BuildController(repo.Object);

        var result = await controller.Get("my-key", CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_NonExistentKey_ReturnsNotFound()
    {
        var repo = new Mock<IWorkflowConfigRepository>();
        repo.Setup(r => r.GetAsync(TenantId, "missing-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowConfig?)null);
        var controller = BuildController(repo.Object);

        var result = await controller.Get("missing-key", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/config
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        var repo = new Mock<IWorkflowConfigRepository>();
        repo.Setup(r => r.GetAsync(TenantId, "new-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowConfig?)null);
        repo.Setup(r => r.CreateAsync(It.IsAny<WorkflowConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = BuildController(repo.Object);

        var result = await controller.Create(
            new CreateConfigRequest("new-key", "new-value", "string", "Description"),
            CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(201);
        repo.Verify(r => r.CreateAsync(
            It.Is<WorkflowConfig>(c => c.Key == "new-key" && c.Value == "new-value" && c.TenantId == TenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_EmptyKey_ReturnsBadRequest()
    {
        var repo = new Mock<IWorkflowConfigRepository>();
        var controller = BuildController(repo.Object);

        var result = await controller.Create(
            new CreateConfigRequest("", "value", null, null),
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_EmptyValue_ReturnsBadRequest()
    {
        var repo = new Mock<IWorkflowConfigRepository>();
        var controller = BuildController(repo.Object);

        var result = await controller.Create(
            new CreateConfigRequest("key", "", null, null),
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_DuplicateKey_ReturnsConflict()
    {
        var existing = MakeConfig("dupe-key");
        var repo = new Mock<IWorkflowConfigRepository>();
        repo.Setup(r => r.GetAsync(TenantId, "dupe-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var controller = BuildController(repo.Object);

        var result = await controller.Create(
            new CreateConfigRequest("dupe-key", "value", null, null),
            CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Create_DefaultsToStringType_WhenValueTypeNotProvided()
    {
        var repo = new Mock<IWorkflowConfigRepository>();
        repo.Setup(r => r.GetAsync(TenantId, "key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowConfig?)null);
        repo.Setup(r => r.CreateAsync(It.IsAny<WorkflowConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = BuildController(repo.Object);

        await controller.Create(
            new CreateConfigRequest("key", "value", null, null),
            CancellationToken.None);

        repo.Verify(r => r.CreateAsync(
            It.Is<WorkflowConfig>(c => c.ValueType == "string"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PUT /api/config/{key}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingConfig_ReturnsOk()
    {
        var config = MakeConfig("my-key", "old-value");
        var repo = new Mock<IWorkflowConfigRepository>();
        repo.Setup(r => r.GetAsync(TenantId, "my-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        repo.Setup(r => r.UpdateAsync(config, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = BuildController(repo.Object);

        var result = await controller.Update("my-key", new UpdateConfigRequest("new-value", "Updated desc"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        repo.Verify(r => r.UpdateAsync(config, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_NonExistentKey_ReturnsNotFound()
    {
        var repo = new Mock<IWorkflowConfigRepository>();
        repo.Setup(r => r.GetAsync(TenantId, "missing-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowConfig?)null);
        var controller = BuildController(repo.Object);

        var result = await controller.Update("missing-key", new UpdateConfigRequest("value", null), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_NullValue_KeepsExistingValue()
    {
        var config = MakeConfig("my-key", "original-value");
        var repo = new Mock<IWorkflowConfigRepository>();
        repo.Setup(r => r.GetAsync(TenantId, "my-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        repo.Setup(r => r.UpdateAsync(config, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = BuildController(repo.Object);

        await controller.Update("my-key", new UpdateConfigRequest(null, "New description"), CancellationToken.None);

        config.Value.Should().Be("original-value");
        repo.Verify(r => r.UpdateAsync(config, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DELETE /api/config/{key}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingKey_ReturnsNoContent()
    {
        var config = MakeConfig("delete-me");
        var repo = new Mock<IWorkflowConfigRepository>();
        repo.Setup(r => r.GetAsync(TenantId, "delete-me", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        repo.Setup(r => r.DeleteAsync(TenantId, "delete-me", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = BuildController(repo.Object);

        var result = await controller.Delete("delete-me", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        repo.Verify(r => r.DeleteAsync(TenantId, "delete-me", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_NonExistentKey_ReturnsNotFound()
    {
        var repo = new Mock<IWorkflowConfigRepository>();
        repo.Setup(r => r.GetAsync(TenantId, "missing-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowConfig?)null);
        var controller = BuildController(repo.Object);

        var result = await controller.Delete("missing-key", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
