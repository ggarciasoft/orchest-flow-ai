using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Api.Controllers;

[ApiController, Route("api/secrets"), Authorize]
public sealed class SecretsController : ControllerBase
{
    private readonly ISecretRepository _repo;
    private readonly IEncryptionService _encryption;

    public SecretsController(ISecretRepository repo, IEncryptionService encryption)
    { _repo = repo; _encryption = encryption; }

    /// <summary>Lists secrets for the tenant — names and metadata only, never values.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var secrets = await _repo.ListAsync(tenantId, ct);
        return Ok(secrets.Select(s => new { s.Id, s.Name, s.CreatedAt, s.UpdatedAt }));
    }

    /// <summary>Creates a new secret. Value is encrypted before storage.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSecretRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Value))
            return BadRequest(new { detail = "name and value are required" });

        var tenantId = GetTenantId();
        var existing = await _repo.GetByNameAsync(req.Name, tenantId, ct);
        if (existing != null)
            return Conflict(new { detail = $"Secret '{req.Name}' already exists" });

        var encrypted = _encryption.Encrypt(req.Value);
        var secret = Secret.Create(tenantId, req.Name, encrypted);
        await _repo.CreateAsync(secret, ct);
        return CreatedAtAction(nameof(List), new { }, new { secret.Id, secret.Name, secret.CreatedAt });
    }

    /// <summary>Updates a secret's value (and optionally its name).</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSecretRequest req, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var secret = await _repo.GetAsync(id, tenantId, ct);
        if (secret == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(req.Value))
            secret.UpdateValue(_encryption.Encrypt(req.Value));
        if (!string.IsNullOrWhiteSpace(req.Name))
            secret.Rename(req.Name);

        await _repo.UpdateAsync(secret, ct);
        return NoContent();
    }

    /// <summary>Deletes a secret.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        await _repo.DeleteAsync(id, tenantId, ct);
        return NoContent();
    }

    private Guid GetTenantId()
    {
        var raw = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(raw, out var g) ? g : Guid.Parse("00000000-0000-0000-0000-000000000001");
    }
}

public sealed record CreateSecretRequest(string Name, string Value);
public sealed record UpdateSecretRequest(string? Name, string? Value);
