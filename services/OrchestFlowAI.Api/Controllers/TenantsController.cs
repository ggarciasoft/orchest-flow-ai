using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Requests;
using OrchestFlowAI.Contracts.Responses;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
using OrchestFlowAI.Infrastructure.Auth;

namespace OrchestFlowAI.Api.Controllers;

/// <summary>
/// Manages tenant creation, retrieval, and team-member invitation flows.
/// Tenant resolution uses the authenticated user's JWT claims — tenant_id is never accepted from the request body.
/// </summary>
[ApiController, Route("api/tenants"), Authorize]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantRepository _tenants;
    private readonly ITenantInviteRepository _invites;
    private readonly IUserRepository _users;
    private readonly JwtTokenService _jwt;
    private readonly IConfiguration _config;

    /// <summary>Initializes the controller with required repositories and auth dependencies.</summary>
    public TenantsController(
        ITenantRepository tenants,
        ITenantInviteRepository invites,
        IUserRepository users,
        JwtTokenService jwt,
        IConfiguration config)
    {
        _tenants = tenants;
        _invites = invites;
        _users = users;
        _jwt = jwt;
        _config = config;
    }

    /// <summary>Extracts the tenant id from the JWT tenant_id claim.</summary>
    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());

    /// <summary>
    /// Creates a new tenant workspace.
    /// </summary>
    /// <param name="req">The workspace name.</param>
    /// <response code="201">Tenant created successfully.</response>
    /// <response code="400">Validation error in request body.</response>
    [HttpPost, Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(TenantResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<TenantResponse>> Create([FromBody] CreateTenantRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Tenant name is required.");

        var tenant = Tenant.Create(req.Name);
        await _tenants.CreateAsync(tenant, ct);

        return CreatedAtAction(nameof(Get), new { id = tenant.Id },
            new TenantResponse(tenant.Id, tenant.Name, tenant.CreatedAt));
    }

    /// <summary>
    /// Gets tenant information by id.
    /// The authenticated user must belong to the requested tenant.
    /// </summary>
    /// <param name="id">The tenant id.</param>
    /// <response code="200">Tenant found.</response>
    /// <response code="403">Tenant id does not match the authenticated user's tenant.</response>
    /// <response code="404">Tenant not found.</response>
    [HttpGet("{id}"), Authorize(Policy = "ViewerOrAbove")]
    [ProducesResponseType(typeof(TenantResponse), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TenantResponse>> Get(Guid id, CancellationToken ct)
    {
        // Tenant id must match the caller's own tenant — no cross-tenant read
        if (id != TenantId) return Forbid();

        var tenant = await _tenants.GetAsync(id, ct);
        if (tenant == null) return NotFound();

        return Ok(new TenantResponse(tenant.Id, tenant.Name, tenant.CreatedAt));
    }

    /// <summary>
    /// Invites a user to the specified tenant by email.
    /// Returns the invite token (MVP: token would be emailed in production).
    /// </summary>
    /// <param name="id">The tenant id.</param>
    /// <param name="req">Email and role for the invitee.</param>
    /// <response code="201">Invite created; token returned in response.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="403">Caller is not a member of this tenant.</response>
    [HttpPost("{id}/invite"), Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(TenantInviteResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<TenantInviteResponse>> Invite(Guid id, [FromBody] InviteUserRequest req, CancellationToken ct)
    {
        if (id != TenantId) return Forbid();

        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest("Email is required.");
        if (string.IsNullOrWhiteSpace(req.Role))
            return BadRequest("Role is required.");

        var invite = TenantInvite.Create(id, req.Email, req.Role);
        await _invites.CreateAsync(invite, ct);

        return StatusCode(201, new TenantInviteResponse(
            invite.Id, invite.TenantId, invite.Email, invite.Role, invite.Token, invite.ExpiresAt));
    }

    /// <summary>
    /// Accepts a tenant invite and creates a new user account.
    /// The invite token must be valid and not expired.
    /// </summary>
    /// <param name="id">The tenant id.</param>
    /// <param name="req">The invite token and the desired password for the new account.</param>
    /// <response code="200">Account created; user may now log in.</response>
    /// <response code="400">Token invalid, expired, or already used.</response>
    [HttpPost("{id}/invite/accept"), AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> AcceptInvite(Guid id, [FromBody] AcceptInviteRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Token))
            return BadRequest("Token is required.");
        if (string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Password is required.");

        var invite = await _invites.GetByTokenAsync(req.Token, ct);
        if (invite == null || invite.TenantId != id)
            return BadRequest("Invalid invite token.");

        if (invite.IsExpired)
            return BadRequest("Invite has expired.");
        if (invite.IsAccepted)
            return BadRequest("Invite has already been accepted.");

        invite.Accept();
        await _invites.UpdateAsync(invite, ct);

        // Hash password before storing
        var hashBytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(req.Password));
        var passwordHash = Convert.ToHexString(hashBytes);

        // Parse role — default to Viewer if unrecognized
        if (!Enum.TryParse<UserRole>(invite.Role, ignoreCase: true, out var role))
            role = UserRole.Viewer;

        var displayName = invite.Email.Split('@')[0];
        var user = OrchestFlowAI.Domain.Entities.User.Create(invite.TenantId, invite.Email, displayName, role, passwordHash);
        await _users.CreateAsync(user, ct);

        return Ok(new { message = "Account created. You may now log in." });
    }

    /// <summary>
    /// Gets the configuration for the caller's tenant.
    /// </summary>
    [HttpGet("{id}/config"), Authorize(Policy = "ViewerOrAbove")]
    public async Task<ActionResult<TenantConfigResponse>> GetConfig(Guid id, CancellationToken ct)
    {
        if (id != TenantId) return Forbid();
        var tenant = await _tenants.GetAsync(id, ct);
        if (tenant == null) return NotFound();
        return Ok(ToConfigResponse(tenant.GetConfig()));
    }

    /// <summary>
    /// Updates the configuration for the caller's tenant. Null fields are left unchanged.
    /// </summary>
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

    private static TenantConfigResponse ToConfigResponse(OrchestFlowAI.Domain.ValueObjects.TenantConfig c)
        => new(c.DisplayName, c.LogoUrl, c.MaxConcurrentExecutions, c.ExecutionTimeoutSeconds, c.DefaultTimezone, c.AllowGuestFormFill);
}
