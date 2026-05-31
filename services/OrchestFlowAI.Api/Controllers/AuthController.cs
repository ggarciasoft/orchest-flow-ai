using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Requests;
using OrchestFlowAI.Contracts.Responses;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
using OrchestFlowAI.Infrastructure.Auth;
using OrchestFlowAI.Infrastructure.Email;

namespace OrchestFlowAI.Api.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly ITenantRepository _tenants;
    private readonly JwtTokenService _jwt;
    private readonly IConfiguration _config;
    private readonly IEmailService _email;

    public AuthController(IUserRepository users, ITenantRepository tenants, JwtTokenService jwt, IConfiguration config, IEmailService email)
    { _users = users; _tenants = tenants; _jwt = jwt; _config = config; _email = email; }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email and password are required.");

        // Look up by email across all tenants (each user now lives in their own tenant)
        var user = await _users.GetByEmailGlobalAsync(req.Email.Trim().ToLowerInvariant(), ct);
        if (user == null)
            return Unauthorized(new { error = "Invalid email or password." });

        var hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(req.Password));
        var hash = Convert.ToHexString(hashBytes);
        if (!string.Equals(user.PasswordHash, hash, StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { error = "Invalid email or password." });

        var key = _config["Auth:JwtSigningKey"] ?? "dev-signing-key-change-in-production-32chars";
        var token = _jwt.GenerateToken(user, key, _config["Auth:JwtIssuer"] ?? "OrchestFlowAI", _config["Auth:JwtAudience"] ?? "OrchestFlowAI-web");
        return Ok(new AuthResponse(token, new UserDto(user.Id, user.Email, user.DisplayName, user.Role.ToString())));
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.DisplayName)) return BadRequest("Display name is required.");
        if (string.IsNullOrWhiteSpace(req.Email))       return BadRequest("Email is required.");
        if (string.IsNullOrWhiteSpace(req.Password))    return BadRequest("Password is required.");
        if (req.Password.Length < 8)                    return BadRequest("Password must be at least 8 characters.");

        // Create a new isolated tenant for this registration
        var tenant = Tenant.Create(req.DisplayName.Trim());
        await _tenants.CreateAsync(tenant, ct);

        // Check uniqueness within the new tenant (fresh tenant = always unique, but guard anyway)
        var existing = await _users.GetByEmailAsync(req.Email, tenant.Id, ct);
        if (existing != null)
            return Conflict(new { error = "An account with that email already exists." });

        var hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(req.Password));
        var hash = Convert.ToHexString(hashBytes);
        var user = OrchestFlowAI.Domain.Entities.User.Create(tenant.Id, req.Email.Trim().ToLowerInvariant(), req.DisplayName.Trim(), UserRole.Admin, hash);
        await _users.CreateAsync(user, ct);

        var key = _config["Auth:JwtSigningKey"] ?? "dev-signing-key-change-in-production-32chars";
        var token = _jwt.GenerateToken(user, key, _config["Auth:JwtIssuer"] ?? "OrchestFlowAI", _config["Auth:JwtAudience"] ?? "OrchestFlowAI-web");

        // Send welcome email (fire-and-forget; do not fail registration if email fails)
        var webBase = _config["App:WebBaseUrl"]?.TrimEnd('/') ?? "http://localhost:3000";
        _ = Task.Run(async () =>
        {
            try
            {
                await _email.SendAsync(
                    user.Email,
                    EmailTemplates.WelcomeSubject(),
                    EmailTemplates.WelcomeHtml(user.DisplayName, webBase),
                    EmailTemplates.WelcomeText(user.DisplayName, webBase));
            }
            catch { /* swallow — welcome email is best-effort */ }
        });

        return StatusCode(201, new AuthResponse(token, new UserDto(user.Id, user.Email, user.DisplayName, user.Role.ToString())));
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
