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

/// <summary>
/// Manages tenant creation, configuration, member management, and invitation flows.
/// Tenant resolution always uses the authenticated user's JWT claims — tenant_id is never accepted from the request body.
/// </summary>
[ApiController, Route("api/tenants"), Authorize]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantRepository _tenants;
    private readonly ITenantInviteRepository _invites;
    private readonly IUserRepository _users;
    private readonly JwtTokenService _jwt;
    private readonly IConfiguration _config;
    private readonly IEmailService _email;

    public TenantsController(
        ITenantRepository tenants,
        ITenantInviteRepository invites,
        IUserRepository users,
        JwtTokenService jwt,
        IConfiguration config,
        IEmailService email)
    {
        _tenants = tenants;
        _invites = invites;
        _users   = users;
        _jwt     = jwt;
        _config  = config;
        _email   = email;
    }

    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());

    // ── Tenant CRUD ──────────────────────────────────────────────────────────

    [HttpPost, Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(TenantResponse), 201)]
    public async Task<ActionResult<TenantResponse>> Create([FromBody] CreateTenantRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Tenant name is required.");

        var tenant = Tenant.Create(req.Name);
        await _tenants.CreateAsync(tenant, ct);
        return CreatedAtAction(nameof(Get), new { id = tenant.Id },
            new TenantResponse(tenant.Id, tenant.Name, tenant.CreatedAt));
    }

    [HttpGet("{id}"), Authorize(Policy = "ViewerOrAbove")]
    [ProducesResponseType(typeof(TenantResponse), 200)]
    public async Task<ActionResult<TenantResponse>> Get(Guid id, CancellationToken ct)
    {
        if (id != TenantId) return Forbid();
        var tenant = await _tenants.GetAsync(id, ct);
        if (tenant == null) return NotFound();
        return Ok(new TenantResponse(tenant.Id, tenant.Name, tenant.CreatedAt));
    }

    // ── Member management ────────────────────────────────────────────────────

    /// <summary>Lists all users in the caller's tenant.</summary>
    [HttpGet("{id}/members"), Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<TenantMemberResponse>), 200)]
    public async Task<ActionResult<IReadOnlyList<TenantMemberResponse>>> ListMembers(Guid id, CancellationToken ct)
    {
        if (id != TenantId) return Forbid();
        var members = await _users.ListByTenantAsync(id, ct);
        return Ok(members.Select(u => new TenantMemberResponse(u.Id, u.Email, u.DisplayName, u.Role.ToString(), u.CreatedAt)));
    }

    /// <summary>Changes the role of an existing member. Admins cannot demote themselves.</summary>
    [HttpPut("{id}/members/{userId}/role"), Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> UpdateMemberRole(Guid id, Guid userId, [FromBody] UpdateMemberRoleRequest req, CancellationToken ct)
    {
        if (id != TenantId) return Forbid();

        var callerId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        if (userId == callerId)
            return BadRequest("You cannot change your own role.");

        if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var role))
            return BadRequest($"Invalid role '{req.Role}'. Valid values: Admin, Editor, Approver, Viewer.");

        await _users.UpdateRoleAsync(userId, id, role, ct);
        return NoContent();
    }

    /// <summary>Removes a member from the tenant. Admins cannot remove themselves.</summary>
    [HttpDelete("{id}/members/{userId}"), Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct)
    {
        if (id != TenantId) return Forbid();

        var callerId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        if (userId == callerId)
            return BadRequest("You cannot remove yourself from the tenant.");

        await _users.DeleteAsync(userId, id, ct);
        return NoContent();
    }

    // ── Invite management ────────────────────────────────────────────────────

    /// <summary>
    /// Lists all pending (not yet accepted) invites for the tenant.
    /// </summary>
    [HttpGet("{id}/invites"), Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<TenantInviteResponse>), 200)]
    public async Task<ActionResult<IReadOnlyList<TenantInviteResponse>>> ListInvites(Guid id, CancellationToken ct)
    {
        if (id != TenantId) return Forbid();
        var invites = await _invites.ListByTenantAsync(id, ct);
        return Ok(invites.Select(ToInviteResponse));
    }

    /// <summary>
    /// Invites a user to the tenant. Sends an email with the accept link.
    /// </summary>
    [HttpPost("{id}/invite"), Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(TenantInviteResponse), 201)]
    public async Task<ActionResult<TenantInviteResponse>> Invite(Guid id, [FromBody] InviteUserRequest req, CancellationToken ct)
    {
        if (id != TenantId) return Forbid();

        var email = req.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email))
            return BadRequest("Email is required.");
        if (string.IsNullOrWhiteSpace(req.Role))
            return BadRequest("Role is required.");
        if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out _))
            return BadRequest($"Invalid role '{req.Role}'. Valid values: Admin, Editor, Approver, Viewer.");

        // Prevent inviting existing members
        var existing = await _users.GetByEmailAsync(email, id, ct);
        if (existing != null)
            return Conflict(new { error = "A user with that email already belongs to this tenant." });

        // Prevent duplicate pending invites
        var pendingInvites = await _invites.ListByTenantAsync(id, ct);
        if (pendingInvites.Any(i => i.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && !i.IsExpired))
            return Conflict(new { error = "An active invite for that email already exists." });

        var tenant = await _tenants.GetAsync(id, ct);
        var invite = TenantInvite.Create(id, email, req.Role);
        await _invites.CreateAsync(invite, ct);

        // Send invite email
        var webBase = _config["App:WebBaseUrl"]?.TrimEnd('/') ?? "http://localhost:3000";
        var acceptUrl = $"{webBase}/invite/{id}?token={invite.Token}";
        var workspaceName = tenant?.Name ?? "OrchestFlowAI";

        try
        {
            await _email.SendAsync(
                email,
                EmailTemplates.InviteSubject(workspaceName),
                EmailTemplates.InviteHtml(workspaceName, req.Role, acceptUrl, invite.ExpiresAt),
                EmailTemplates.InviteText(workspaceName, req.Role, acceptUrl, invite.ExpiresAt),
                ct);
        }
        catch (Exception ex)
        {
            // Log but don't fail the request — the admin can share the link manually
            HttpContext.RequestServices
                .GetService<Microsoft.Extensions.Logging.ILogger<TenantsController>>()
                ?.LogWarning(ex, "Failed to send invite email to {Email}", email);
        }

        return StatusCode(201, ToInviteResponse(invite));
    }

    /// <summary>
    /// Revokes (deletes) a pending invite.
    /// </summary>
    [HttpDelete("{id}/invites/{inviteId}"), Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> RevokeInvite(Guid id, Guid inviteId, CancellationToken ct)
    {
        if (id != TenantId) return Forbid();
        await _invites.DeleteAsync(inviteId, id, ct);
        return NoContent();
    }

    /// <summary>
    /// Public endpoint that returns invite metadata for the accept page.
    /// Returns only non-secret information (email, tenant name, role, expiry).
    /// </summary>
    [HttpGet("{id}/invite/preview"), AllowAnonymous]
    [ProducesResponseType(typeof(InvitePreviewResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<InvitePreviewResponse>> InvitePreview(Guid id, [FromQuery] string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest("Token is required.");

        var invite = await _invites.GetByTokenAsync(token, ct);
        if (invite == null || invite.TenantId != id || invite.IsAccepted || invite.IsExpired)
            return NotFound(new { error = "Invite not found, expired, or already used." });

        var tenant = await _tenants.GetAsync(id, ct);
        return Ok(new InvitePreviewResponse(invite.Email, tenant?.Name ?? "OrchestFlowAI", invite.Role, invite.ExpiresAt));
    }

    /// <summary>
    /// Accepts an invite, creates the user account, and returns a JWT for immediate login.
    /// </summary>
    [HttpPost("{id}/invite/accept"), AllowAnonymous]
    [ProducesResponseType(typeof(AcceptInviteResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<AcceptInviteResponse>> AcceptInvite(Guid id, [FromBody] AcceptInviteRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Token))    return BadRequest("Token is required.");
        if (string.IsNullOrWhiteSpace(req.Password)) return BadRequest("Password is required.");
        if (req.Password.Length < 8)                 return BadRequest("Password must be at least 8 characters.");

        var invite = await _invites.GetByTokenAsync(req.Token, ct);
        if (invite == null || invite.TenantId != id) return BadRequest("Invalid invite token.");
        if (invite.IsExpired)                         return BadRequest("Invite has expired.");
        if (invite.IsAccepted)                        return BadRequest("Invite has already been accepted.");

        // Check if a user with this email already exists in the tenant (race condition guard)
        var existingUser = await _users.GetByEmailAsync(invite.Email, id, ct);
        if (existingUser != null)
            return BadRequest("An account with that email already exists in this workspace.");

        invite.Accept();
        await _invites.UpdateAsync(invite, ct);

        var hashBytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(req.Password));
        var passwordHash = Convert.ToHexString(hashBytes);

        if (!Enum.TryParse<UserRole>(invite.Role, ignoreCase: true, out var role))
            role = UserRole.Viewer;

        var displayName = invite.Email.Split('@')[0];
        var user = OrchestFlowAI.Domain.Entities.User.Create(invite.TenantId, invite.Email, displayName, role, passwordHash);
        await _users.CreateAsync(user, ct);

        // Return a JWT so the client can auto-login
        var key = _config["Auth:JwtSigningKey"] ?? "dev-signing-key-change-in-production-32chars";
        var token = _jwt.GenerateToken(user, key,
            _config["Auth:JwtIssuer"] ?? "OrchestFlowAI",
            _config["Auth:JwtAudience"] ?? "OrchestFlowAI-web");

        return Ok(new AcceptInviteResponse(token, new UserDto(user.Id, user.Email, user.DisplayName, user.Role.ToString())));
    }

    // ── Tenant config ────────────────────────────────────────────────────────

    [HttpGet("{id}/config"), Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult<TenantConfigResponse>> GetConfig(Guid id, CancellationToken ct)
    {
        if (id != TenantId) return Forbid();
        var tenant = await _tenants.GetAsync(id, ct);
        if (tenant == null) return NotFound();
        return Ok(ToConfigResponse(tenant.GetConfig()));
    }

    [HttpPut("{id}/config"), Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<TenantConfigResponse>> UpdateConfig(Guid id, [FromBody] UpdateTenantConfigRequest req, CancellationToken ct)
    {
        if (id != TenantId) return Forbid();
        var tenant = await _tenants.GetAsync(id, ct);
        if (tenant == null) return NotFound();

        var config = tenant.GetConfig();
        if (req.DisplayName != null)             config.DisplayName = req.DisplayName;
        if (req.LogoUrl != null)                 config.LogoUrl = req.LogoUrl;
        if (req.MaxConcurrentExecutions != null) config.MaxConcurrentExecutions = Math.Max(0, req.MaxConcurrentExecutions.Value);
        if (req.ExecutionTimeoutSeconds != null) config.ExecutionTimeoutSeconds = Math.Max(0, req.ExecutionTimeoutSeconds.Value);
        if (req.DefaultTimezone != null)         config.DefaultTimezone = req.DefaultTimezone;
        if (req.AllowGuestFormFill != null)      config.AllowGuestFormFill = req.AllowGuestFormFill.Value;

        tenant.UpdateConfig(config);
        await _tenants.UpdateAsync(tenant, ct);
        return Ok(ToConfigResponse(config));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static TenantInviteResponse ToInviteResponse(TenantInvite i)
        => new(i.Id, i.TenantId, i.Email, i.Role, i.ExpiresAt);

    private static TenantConfigResponse ToConfigResponse(OrchestFlowAI.Domain.ValueObjects.TenantConfig c)
        => new(c.DisplayName, c.LogoUrl, c.MaxConcurrentExecutions, c.ExecutionTimeoutSeconds, c.DefaultTimezone, c.AllowGuestFormFill);
}
