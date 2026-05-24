namespace OrchestAI.Contracts.Responses;

/// <summary>Response DTO for a tenant resource.</summary>
public sealed record TenantResponse(Guid Id, string Name, DateTime CreatedAt);

/// <summary>Response DTO for a tenant invite — includes the token for the MVP flow.</summary>
public sealed record TenantInviteResponse(Guid Id, Guid TenantId, string Email, string Role, string Token, DateTime ExpiresAt);
