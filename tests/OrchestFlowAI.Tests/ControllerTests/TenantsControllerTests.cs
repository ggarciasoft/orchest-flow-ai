using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using OrchestFlowAI.Api.Controllers;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Requests;
using OrchestFlowAI.Contracts.Responses;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
using OrchestFlowAI.Infrastructure.Auth;
using System.Security.Claims;

namespace OrchestFlowAI.Tests.ControllerTests;

/// <summary>
/// Unit tests for <see cref="TenantsController"/> — covers create tenant, invite, and accept-invite flows.
/// </summary>
public sealed class TenantsControllerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // ────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────

    private static TenantsController BuildController(
        Mock<ITenantRepository>? tenants = null,
        Mock<ITenantInviteRepository>? invites = null,
        Mock<IUserRepository>? users = null,
        Guid? tenantId = null)
    {
        var tenantRepo = tenants ?? new Mock<ITenantRepository>();
        var inviteRepo = invites ?? new Mock<ITenantInviteRepository>();
        var userRepo = users ?? new Mock<IUserRepository>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:JwtSigningKey"] = "test-signing-key-must-be-long-enough-32ch",
                ["Auth:JwtIssuer"] = "test",
                ["Auth:JwtAudience"] = "test",
            })
            .Build();

        var jwt = new JwtTokenService();

        var controller = new TenantsController(tenantRepo.Object, inviteRepo.Object, userRepo.Object, jwt, config);

        // Set up fake authenticated user claims
        var effectiveTenantId = tenantId ?? TenantId;
        var claims = new[]
        {
            new Claim("tenant_id", effectiveTenantId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
            new Claim(ClaimTypes.Role, "Admin"),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };

        return controller;
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/tenants
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201WithTenantResponse()
    {
        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(r => r.CreateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant t, CancellationToken _) => t);

        var controller = BuildController(tenants: tenantRepo);

        var result = await controller.Create(new CreateTenantRequest("Acme Inc."), CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = created.Value.Should().BeOfType<TenantResponse>().Subject;
        response.Name.Should().Be("Acme Inc.");
        response.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_EmptyName_ReturnsBadRequest()
    {
        var controller = BuildController();

        var result = await controller.Create(new CreateTenantRequest("  "), CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_CallsRepository()
    {
        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(r => r.CreateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant t, CancellationToken _) => t);
        var controller = BuildController(tenants: tenantRepo);

        await controller.Create(new CreateTenantRequest("Test Tenant"), CancellationToken.None);

        tenantRepo.Verify(r => r.CreateAsync(It.Is<Tenant>(t => t.Name == "Test Tenant"), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ────────────────────────────────────────────────────────────────────────
    // GET /api/tenants/{id}
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ExistingTenant_ReturnsOk()
    {
        var tenant = Tenant.Create("My Tenant");
        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(r => r.GetAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var controller = BuildController(tenants: tenantRepo);

        var result = await controller.Get(TenantId, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Get_NotFound_Returns404()
    {
        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(r => r.GetAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var controller = BuildController(tenants: tenantRepo);

        var result = await controller.Get(TenantId, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Get_DifferentTenantId_ReturnsForbid()
    {
        var differentId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var controller = BuildController(); // controller is authenticated as TenantId

        var result = await controller.Get(differentId, CancellationToken.None);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/tenants/{id}/invite
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Invite_ValidRequest_Returns201WithToken()
    {
        var inviteRepo = new Mock<ITenantInviteRepository>();
        inviteRepo.Setup(r => r.CreateAsync(It.IsAny<TenantInvite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantInvite i, CancellationToken _) => i);

        var controller = BuildController(invites: inviteRepo);

        var result = await controller.Invite(TenantId, new InviteUserRequest("bob@example.com", "Viewer"), CancellationToken.None);

        var status = result.Result.Should().BeOfType<ObjectResult>().Subject;
        status.StatusCode.Should().Be(201);
        var response = status.Value.Should().BeOfType<TenantInviteResponse>().Subject;
        response.Email.Should().Be("bob@example.com");
        response.Role.Should().Be("Viewer");
        response.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Invite_MissingEmail_ReturnsBadRequest()
    {
        var controller = BuildController();

        var result = await controller.Invite(TenantId, new InviteUserRequest("", "Viewer"), CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Invite_DifferentTenantId_ReturnsForbid()
    {
        var differentId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var controller = BuildController();

        var result = await controller.Invite(differentId, new InviteUserRequest("bob@example.com", "Viewer"), CancellationToken.None);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/tenants/{id}/invite/accept
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AcceptInvite_ValidToken_Returns200AndCreatesUser()
    {
        var invite = TenantInvite.Create(TenantId, "carol@example.com", "Editor");

        var inviteRepo = new Mock<ITenantInviteRepository>();
        inviteRepo.Setup(r => r.GetByTokenAsync(invite.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);
        inviteRepo.Setup(r => r.UpdateAsync(invite, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.CreateAsync(It.IsAny<OrchestFlowAI.Domain.Entities.User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrchestFlowAI.Domain.Entities.User u, CancellationToken _) => u);

        var controller = BuildController(invites: inviteRepo, users: userRepo);

        var result = await controller.AcceptInvite(TenantId, new AcceptInviteRequest(invite.Token, "Secure1234"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        userRepo.Verify(r => r.CreateAsync(
            It.Is<OrchestFlowAI.Domain.Entities.User>(u => u.Email == "carol@example.com" && u.Role == UserRole.Editor),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptInvite_InvalidToken_ReturnsBadRequest()
    {
        var inviteRepo = new Mock<ITenantInviteRepository>();
        inviteRepo.Setup(r => r.GetByTokenAsync("bad-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantInvite?)null);

        var controller = BuildController(invites: inviteRepo);

        var result = await controller.AcceptInvite(TenantId, new AcceptInviteRequest("bad-token", "Password1"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AcceptInvite_AlreadyAccepted_ReturnsBadRequest()
    {
        var invite = TenantInvite.Create(TenantId, "dave@example.com", "Viewer");
        invite.Accept();

        var inviteRepo = new Mock<ITenantInviteRepository>();
        inviteRepo.Setup(r => r.GetByTokenAsync(invite.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        var controller = BuildController(invites: inviteRepo);

        var result = await controller.AcceptInvite(TenantId, new AcceptInviteRequest(invite.Token, "Password1"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AcceptInvite_MissingPassword_ReturnsBadRequest()
    {
        var controller = BuildController();

        var result = await controller.AcceptInvite(TenantId, new AcceptInviteRequest("some-token", ""), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AcceptInvite_WrongTenantId_ReturnsBadRequest()
    {
        var invite = TenantInvite.Create(TenantId, "eve@example.com", "Viewer");
        var differentTenantId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var inviteRepo = new Mock<ITenantInviteRepository>();
        inviteRepo.Setup(r => r.GetByTokenAsync(invite.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        var controller = BuildController(invites: inviteRepo);

        var result = await controller.AcceptInvite(differentTenantId, new AcceptInviteRequest(invite.Token, "Password1"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
