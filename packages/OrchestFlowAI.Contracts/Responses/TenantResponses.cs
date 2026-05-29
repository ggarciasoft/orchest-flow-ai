namespace OrchestFlowAI.Contracts.Responses;

/// <summary>Response DTO for a tenant resource.</summary>
public sealed record TenantResponse(Guid Id, string Name, DateTime CreatedAt);

/// <summary>Response DTO for a tenant invite — includes the token for the MVP flow.</summary>
public sealed record TenantInviteResponse(Guid Id, Guid TenantId, string Email, string Role, string Token, DateTime ExpiresAt);

/// <summary>Response DTO for tenant configuration.</summary>
public sealed record TenantConfigResponse(
    string? DisplayName,
    string? LogoUrl,
    int MaxConcurrentExecutions,
    int ExecutionTimeoutSeconds,
    string DefaultTimezone,
    bool AllowGuestFormFill
);
