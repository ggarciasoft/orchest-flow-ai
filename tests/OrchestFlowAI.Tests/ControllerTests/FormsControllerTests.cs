using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrchestFlowAI.Api.Controllers;
using OrchestFlowAI.Api.Services;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Requests;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Engine.Registry;
using OrchestFlowAI.Infrastructure.Persistence;
using OrchestFlowAI.Infrastructure.Repositories;
using OrchestFlowAI.SDK.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace OrchestFlowAI.Tests.ControllerTests;

public sealed class FormsControllerTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static OrchestFlowAIDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<OrchestFlowAIDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrchestFlowAIDbContext(options);
    }

    private static FormsController BuildController(IFormRepository repo, IExecutionQueue? queue = null, FormNodeRegistrar? registrar = null, IExecutionRepository? executions = null)
    {
        queue ??= Mock.Of<IExecutionQueue>();
        registrar ??= BuildRegistrar(repo);
        executions ??= Mock.Of<IExecutionRepository>();
        var approvals = Mock.Of<IApprovalRepository>();
        var ctrl = new FormsController(repo, queue, registrar, executions, approvals);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim("tenant_id", TenantId.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
                }, "Test"))
            }
        };
        return ctrl;
    }

    private static FormNodeRegistrar BuildRegistrar(IFormRepository? repo = null)
    {
        var registryMock = new Mock<INodeRegistry>();
        registryMock.Setup(r => r.GetAllDescriptors()).Returns(Array.Empty<IWorkflowNodeDescriptor>());
        var registry = registryMock.Object;
        var logger = NullLogger<FormNodeRegistrar>.Instance;

        if (repo == null)
            return new FormNodeRegistrar(Mock.Of<IServiceScopeFactory>(), registry, logger);

        // Set up scope factory to return repo via scope
        var sp = new Mock<IServiceProvider>();
        sp.Setup(x => x.GetService(typeof(IFormRepository))).Returns(repo);
        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(sp.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);
        return new FormNodeRegistrar(scopeFactory.Object, registry, logger);
    }

    private static StubFormRepository BuildStubRepo() => new();

    // ────────────────────────────────────────────────────────────────────────
    // List
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_EmptyTenant_ReturnsEmptyList()
    {
        var repo = BuildStubRepo();
        var ctrl = BuildController(repo);
        var result = await ctrl.List(CancellationToken.None);
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IReadOnlyList<object>>().Subject;
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task List_WithForms_ReturnsOnlyTenantForms()
    {
        var repo = BuildStubRepo();
        var form1 = Form.Create(TenantId, "Form A", "form-a", null, "[]");
        var form2 = Form.Create(Guid.NewGuid(), "Other Tenant", "form-b", null, "[]");
        await repo.CreateAsync(form1);
        await repo.CreateAsync(form2);

        var ctrl = BuildController(repo);
        var result = await ctrl.List(CancellationToken.None);
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = (ok.Value as System.Collections.IEnumerable)!.Cast<object>().ToList();
        list.Should().HaveCount(1);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Create
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201WithForm()
    {
        var repo = BuildStubRepo();
        var ctrl = BuildController(repo);
        var fields = JsonDocument.Parse("[{\"key\":\"name\",\"label\":\"Name\",\"type\":\"text\"}]").RootElement;
        var req = new CreateFormRequest("My Form", "my-form", "A test form", fields);

        var result = await ctrl.Create(req, CancellationToken.None);
        var status = result.Result.Should().BeOfType<ObjectResult>().Subject;
        status.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_EmptyName_ReturnsBadRequest()
    {
        var repo = BuildStubRepo();
        var ctrl = BuildController(repo);
        var fields = JsonDocument.Parse("[]").RootElement;
        var result = await ctrl.Create(new CreateFormRequest("", "slug", null, fields), CancellationToken.None);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_EmptySlug_ReturnsBadRequest()
    {
        var repo = BuildStubRepo();
        var ctrl = BuildController(repo);
        var fields = JsonDocument.Parse("[]").RootElement;
        var result = await ctrl.Create(new CreateFormRequest("Name", "  ", null, fields), CancellationToken.None);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Get
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ExistingForm_ReturnsForm()
    {
        var repo = BuildStubRepo();
        var form = Form.Create(TenantId, "Test Form", "test-form", null, "[]");
        await repo.CreateAsync(form);
        var ctrl = BuildController(repo);
        var result = await ctrl.Get(form.Id, CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Get_NonExistentForm_ReturnsNotFound()
    {
        var repo = BuildStubRepo();
        var ctrl = BuildController(repo);
        var result = await ctrl.Get(Guid.NewGuid(), CancellationToken.None);
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Update
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingForm_UpdatesAndReturnsForm()
    {
        var repo = BuildStubRepo();
        var form = Form.Create(TenantId, "Old Name", "old-slug", null, "[]");
        await repo.CreateAsync(form);
        var ctrl = BuildController(repo);
        var fields = JsonDocument.Parse("[]").RootElement;
        var result = await ctrl.Update(form.Id, new UpdateFormRequest("New Name", "new-slug", "Desc", fields), CancellationToken.None);
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_NonExistentForm_ReturnsNotFound()
    {
        var repo = BuildStubRepo();
        var ctrl = BuildController(repo);
        var fields = JsonDocument.Parse("[]").RootElement;
        var result = await ctrl.Update(Guid.NewGuid(), new UpdateFormRequest("Name", "slug", null, fields), CancellationToken.None);
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Delete
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingForm_SoftDeletesAndReturnsNoContent()
    {
        var repo = BuildStubRepo();
        var form = Form.Create(TenantId, "To Delete", "to-delete", null, "[]");
        await repo.CreateAsync(form);
        var ctrl = BuildController(repo);
        var result = await ctrl.Delete(form.Id, CancellationToken.None);
        result.Should().BeOfType<NoContentResult>();

        // Form is soft-deleted — should no longer appear in list
        var listResult = await ctrl.List(CancellationToken.None);
        var ok = listResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = (ok.Value as System.Collections.IEnumerable)!.Cast<object>().ToList();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_NonExistentForm_ReturnsNotFound()
    {
        var repo = BuildStubRepo();
        var ctrl = BuildController(repo);
        var result = await ctrl.Delete(Guid.NewGuid(), CancellationToken.None);
        result.Should().BeOfType<NotFoundResult>();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Submit — regex validation
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_WithRegexValidation_InvalidValue_Returns400()
    {
        var repo = BuildStubRepo();
        var fieldDef = new OrchestFlowAI.Domain.Entities.FormFieldDefinition(
            Key: "amount",
            Label: "Amount",
            Type: "text",
            ValidationRegex: @"^[0-9]+(\.[0-9]{1,2})?$",
            ValidationMessage: "Must be a valid decimal number");
        var fieldsJson = System.Text.Json.JsonSerializer.Serialize(new[] { fieldDef });
        var form = Form.Create(TenantId, "Payment", "payment", null, fieldsJson);
        await repo.CreateAsync(form);

        var ctrl = BuildController(repo);
        var valuesJson = System.Text.Json.JsonDocument.Parse("{\"amount\":\"not-a-number\"}").RootElement;
        var nodeExecId = Guid.NewGuid().ToString();
        var req = new OrchestFlowAI.Contracts.Requests.SubmitFormRequest(
            Guid.NewGuid(),
            nodeExecId,
            valuesJson);
        var result = await ctrl.Submit(form.Id, req, CancellationToken.None);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Submit_WithRegexValidation_ValidValue_Returns204()
    {
        var repo = BuildStubRepo();
        var queueMock = new Mock<IExecutionQueue>();
        queueMock
            .Setup(q => q.EnqueueResumeAsync(It.IsAny<OrchestFlowAI.Contracts.Events.ExecutionResumeMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var fieldDef = new OrchestFlowAI.Domain.Entities.FormFieldDefinition(
            Key: "amount",
            Label: "Amount",
            Type: "text",
            ValidationRegex: @"^[0-9]+(\.[0-9]{1,2})?$",
            ValidationMessage: "Must be a valid decimal number");
        var fieldsJson = System.Text.Json.JsonSerializer.Serialize(new[] { fieldDef });
        var form = Form.Create(TenantId, "Payment", "payment", null, fieldsJson);
        await repo.CreateAsync(form);

        var ctrl = BuildController(repo, queueMock.Object);
        var nodeExecId = Guid.NewGuid();
        var valuesJson = System.Text.Json.JsonDocument.Parse($"{{\"amount\":\"12.50\"}}").RootElement;
        var req = new OrchestFlowAI.Contracts.Requests.SubmitFormRequest(
            Guid.NewGuid(),
            nodeExecId.ToString(),
            valuesJson);
        var result = await ctrl.Submit(form.Id, req, CancellationToken.None);
        result.Should().BeOfType<NoContentResult>();
    }
}
