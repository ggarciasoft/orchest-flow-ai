using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestAI.Application.Abstractions;
using OrchestAI.Contracts.Requests;
using OrchestAI.Contracts.Responses;
using OrchestAI.Domain.Entities;
using OrchestAI.Domain.Enums;
using OrchestAI.Infrastructure.Auth;
namespace OrchestAI.Api.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly JwtTokenService _jwt;
    private readonly IConfiguration _config;

    public AuthController(IUserRepository users, JwtTokenService jwt, IConfiguration config)
    { _users = users; _jwt = jwt; _config = config; }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var user = await _users.GetByEmailAsync(req.Email, tenantId, ct);
        if (user == null)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(req.Password);
            var hashBytes = System.Security.Cryptography.SHA256.HashData(bytes);
            var hash = Convert.ToHexString(hashBytes);
            user = OrchestAI.Domain.Entities.User.Create(tenantId, req.Email, req.Email.Split('@')[0], UserRole.Admin, hash);
            await _users.CreateAsync(user, ct);
        }
        var key = _config["Auth:JwtSigningKey"] ?? "dev-signing-key-change-in-production-32chars";
        var token = _jwt.GenerateToken(user, key, _config["Auth:JwtIssuer"] ?? "orchestai", _config["Auth:JwtAudience"] ?? "orchestai-web");
        return Ok(new AuthResponse(token, new UserDto(user.Id, user.Email, user.DisplayName, user.Role.ToString())));
    }

    [HttpGet("me"), Authorize]
    public ActionResult<UserDto> Me()
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        var display = User.FindFirst("display_name")?.Value ?? email;
        return Ok(new UserDto(userId, email, display, role));
    }
}
