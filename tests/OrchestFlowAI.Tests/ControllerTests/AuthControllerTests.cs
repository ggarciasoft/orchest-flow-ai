using FluentAssertions;
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

namespace OrchestFlowAI.Tests.ControllerTests;

public sealed class AuthControllerTests
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static AuthController BuildController(Mock<IUserRepository> users)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:JwtSigningKey"] = "test-signing-key-must-be-long-enough-32ch",
                ["Auth:JwtIssuer"]     = "test",
                ["Auth:JwtAudience"]   = "test",
            })
            .Build();
        return new AuthController(users.Object, new JwtTokenService(), config);
    }

    private static string HashPassword(string password)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    // ── Login ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var user = User.Create(DefaultTenantId, "alice@test.com", "Alice", UserRole.Admin, HashPassword("password123"));
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByEmailAsync("alice@test.com", DefaultTenantId, default))
             .ReturnsAsync(user);

        var ctrl = BuildController(users);
        var result = await ctrl.Login(new LoginRequest("alice@test.com", "password123"), default);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var resp = ok.Value.Should().BeAssignableTo<AuthResponse>().Subject;
        resp.Token.Should().NotBeNullOrEmpty();
        resp.User.Email.Should().Be("alice@test.com");
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), DefaultTenantId, default))
             .ReturnsAsync((User?)null);

        var ctrl = BuildController(users);
        var result = await ctrl.Login(new LoginRequest("ghost@test.com", "anything"), default);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var user = User.Create(DefaultTenantId, "alice@test.com", "Alice", UserRole.Admin, HashPassword("correct"));
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByEmailAsync("alice@test.com", DefaultTenantId, default))
             .ReturnsAsync(user);

        var ctrl = BuildController(users);
        var result = await ctrl.Login(new LoginRequest("alice@test.com", "wrong"), default);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_EmptyEmail_Returns400()
    {
        var ctrl = BuildController(new Mock<IUserRepository>());
        var result = await ctrl.Login(new LoginRequest("", "password"), default);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Register ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_NewUser_Returns201WithToken()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByEmailAsync("newuser@test.com", DefaultTenantId, default))
             .ReturnsAsync((User?)null);
        users.Setup(r => r.CreateAsync(It.IsAny<User>(), default))
             .ReturnsAsync((User u, CancellationToken _) => u);

        var ctrl = BuildController(users);
        var result = await ctrl.Register(new RegisterRequest("New User", "newuser@test.com", "password123"), default);

        var status = result.Result.Should().BeOfType<ObjectResult>().Subject;
        status.StatusCode.Should().Be(201);
        var resp = status.Value.Should().BeAssignableTo<AuthResponse>().Subject;
        resp.Token.Should().NotBeNullOrEmpty();
        resp.User.Email.Should().Be("newuser@test.com");
        resp.User.DisplayName.Should().Be("New User");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var existing = User.Create(DefaultTenantId, "taken@test.com", "Old User", UserRole.Admin, HashPassword("pass"));
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByEmailAsync("taken@test.com", DefaultTenantId, default))
             .ReturnsAsync(existing);

        var ctrl = BuildController(users);
        var result = await ctrl.Register(new RegisterRequest("New", "taken@test.com", "password123"), default);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var ctrl = BuildController(new Mock<IUserRepository>());
        var result = await ctrl.Register(new RegisterRequest("Name", "user@test.com", "short"), default);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_MissingDisplayName_Returns400()
    {
        var ctrl = BuildController(new Mock<IUserRepository>());
        var result = await ctrl.Register(new RegisterRequest("", "user@test.com", "password123"), default);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_MissingEmail_Returns400()
    {
        var ctrl = BuildController(new Mock<IUserRepository>());
        var result = await ctrl.Register(new RegisterRequest("Name", "", "password123"), default);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
