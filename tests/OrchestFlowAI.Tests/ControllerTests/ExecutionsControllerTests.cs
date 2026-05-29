using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrchestFlowAI.Api.Controllers;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Responses;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
using OrchestFlowAI.Engine;
using OrchestFlowAI.Infrastructure.Repositories;
using System.Security.Claims;

namespace OrchestFlowAI.Tests.ControllerTests;

public sealed class ExecutionsControllerTests
{
    private static readonly Guid TenantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid UserId   = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private static ExecutionsController BuildController(
        IExecutionRepository execRepo,
        IWorkflowRepository? wfRepo = null,
        IWorkflowEngine? engine = null)
    {
        wfRepo ??= Mock.Of<IWorkflowRepository>();
        engine ??= Mock.Of<IWorkflowEngine>();
        var ctrl = new ExecutionsController(execRepo, wfRepo, engine);
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

    // Builds a fresh isolated StubExecutionRepository (the stub uses static dictionaries, so we
    // use Moq to avoid cross-test pollution for these tests)
    private static (Mock<IExecutionRepository> mock, List<WorkflowExecution> store) BuildMockRepo()
    {
        var store = new List<WorkflowExecution>();
        var mock = new Mock<IExecutionRepository>();
        mock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => store.FirstOrDefault(e => e.Id == id));
        mock.Setup(r => r.ListAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid tid, string? status, string? search, int page, int pageSize, CancellationToken _) =>
            {
                var q = store.Where(e => e.TenantId == tid);
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<ExecutionStatus>(status, true, out var s)) q = q.Where(e => e.Status == s);
                if (!string.IsNullOrEmpty(search)) q = q.Where(e => e.CorrelationId.Contains(search, StringComparison.OrdinalIgnoreCase));
                return (IReadOnlyList<WorkflowExecution>)q.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            });
        mock.Setup(r => r.CountAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid tid, string? status, string? search, CancellationToken _) =>
            {
                var q = store.Where(e => e.TenantId == tid);
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<ExecutionStatus>(status, true, out var s)) q = q.Where(e => e.Status == s);
                if (!string.IsNullOrEmpty(search)) q = q.Where(e => e.CorrelationId.Contains(search, StringComparison.OrdinalIgnoreCase));
                return q.Count();
            });
        mock.Setup(r => r.UpdateAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return (mock, store);
    }

    private static WorkflowExecution MakeExecution(Guid? tenantId = null, ExecutionStatus status = ExecutionStatus.Queued, string? correlationId = null)
    {
        var e = WorkflowExecution.Create(
            tenantId ?? TenantId,
            Guid.NewGuid(), Guid.NewGuid(), null,
            "{}",
            correlationId ?? Guid.NewGuid().ToString());
        if (status == ExecutionStatus.Running) e.Start();
        else if (status == ExecutionStatus.Completed) { e.Start(); e.Complete(null); }
        else if (status == ExecutionStatus.Failed) { e.Start(); e.Fail("error"); }
        else if (status == ExecutionStatus.Cancelled) { e.Start(); e.Cancel(); }
        return e;
    }

    // ── List ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_NoFilter_ReturnsPagedResultForTenant()
    {
        var (mock, store) = BuildMockRepo();
        store.Add(MakeExecution());
        store.Add(MakeExecution());
        store.Add(MakeExecution(tenantId: Guid.NewGuid())); // different tenant — should be excluded

        var ctrl = BuildController(mock.Object);
        var result = await ctrl.List(null, null, 1, 20, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeAssignableTo<PagedResponse<WorkflowExecutionResponse>>().Subject;
        paged.Items.Should().HaveCount(2);
        paged.Total.Should().Be(2);
    }

    [Fact]
    public async Task List_StatusFilter_ReturnsOnlyMatchingStatus()
    {
        var (mock, store) = BuildMockRepo();
        store.Add(MakeExecution(status: ExecutionStatus.Running));
        store.Add(MakeExecution(status: ExecutionStatus.Running));
        store.Add(MakeExecution(status: ExecutionStatus.Completed));

        var ctrl = BuildController(mock.Object);
        var result = await ctrl.List("Running", null, 1, 20, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeAssignableTo<PagedResponse<WorkflowExecutionResponse>>().Subject;
        paged.Items.Should().HaveCount(2);
        paged.Total.Should().Be(2);
    }

    [Fact]
    public async Task List_SearchFilter_MatchesCorrelationId()
    {
        var (mock, store) = BuildMockRepo();
        store.Add(MakeExecution(correlationId: "abc-unique-123"));
        store.Add(MakeExecution(correlationId: "xyz-other-456"));

        var ctrl = BuildController(mock.Object);
        var result = await ctrl.List(null, "abc-unique", 1, 20, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeAssignableTo<PagedResponse<WorkflowExecutionResponse>>().Subject;
        paged.Items.Should().HaveCount(1);
        paged.Total.Should().Be(1);
    }

    [Fact]
    public async Task List_Pagination_ReturnsCorrectPage()
    {
        var (mock, store) = BuildMockRepo();
        for (var i = 0; i < 5; i++) store.Add(MakeExecution());

        var ctrl = BuildController(mock.Object);
        var p1 = await ctrl.List(null, null, 1, 3, CancellationToken.None);
        var ok1 = p1.Result.Should().BeOfType<OkObjectResult>().Subject;
        var paged1 = ok1.Value.Should().BeAssignableTo<PagedResponse<WorkflowExecutionResponse>>().Subject;
        paged1.Items.Should().HaveCount(3);
        paged1.Total.Should().Be(5);

        var p2 = await ctrl.List(null, null, 2, 3, CancellationToken.None);
        var ok2 = p2.Result.Should().BeOfType<OkObjectResult>().Subject;
        var paged2 = ok2.Value.Should().BeAssignableTo<PagedResponse<WorkflowExecutionResponse>>().Subject;
        paged2.Items.Should().HaveCount(2);
        paged2.Total.Should().Be(5);
    }

    // ── Cancel ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_NotFound_Returns404()
    {
        var (mock, _) = BuildMockRepo();
        var ctrl = BuildController(mock.Object);
        var result = await ctrl.Cancel(Guid.NewGuid(), CancellationToken.None);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Cancel_DifferentTenant_Returns404()
    {
        var (mock, store) = BuildMockRepo();
        // Execution belongs to a different tenant
        var exec = MakeExecution(tenantId: Guid.NewGuid(), status: ExecutionStatus.Running);
        store.Add(exec);

        var ctrl = BuildController(mock.Object);
        var result = await ctrl.Cancel(exec.Id, CancellationToken.None);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Cancel_ActiveExecution_Returns204()
    {
        var (mock, store) = BuildMockRepo();
        var exec = MakeExecution(status: ExecutionStatus.Running);
        store.Add(exec);

        var engineMock = new Mock<IWorkflowEngine>();
        engineMock.Setup(e => e.CancelAsync(exec.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var ctrl = BuildController(mock.Object, engine: engineMock.Object);
        var result = await ctrl.Cancel(exec.Id, CancellationToken.None);
        result.Should().BeOfType<NoContentResult>();
        engineMock.Verify(e => e.CancelAsync(exec.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_TerminalExecution_Returns409()
    {
        var (mock, store) = BuildMockRepo();
        var exec = MakeExecution(status: ExecutionStatus.Completed);
        store.Add(exec);

        var engineMock = new Mock<IWorkflowEngine>();
        engineMock.Setup(e => e.CancelAsync(exec.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Execution is already in terminal state: Completed"));

        var ctrl = BuildController(mock.Object, engine: engineMock.Object);
        var result = await ctrl.Cancel(exec.Id, CancellationToken.None);
        result.Should().BeOfType<ConflictObjectResult>();
    }
}
