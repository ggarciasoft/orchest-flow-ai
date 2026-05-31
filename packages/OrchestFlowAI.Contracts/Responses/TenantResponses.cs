namespace OrchestFlowAI.Contracts.Responses;

/// <summary>Response DTO for a tenant resource.</summary>
public sealed record TenantResponse(Guid Id, string Name, DateTime CreatedAt);

/// <summary>Response DTO for a tenant invite — token omitted; email is sent instead.</summary>
public sealed record TenantInviteResponse(Guid Id, Guid TenantId, string Email, string Role, DateTime ExpiresAt);

/// <summary>Public preview of an invite shown on the accept page; contains no secrets.</summary>
public sealed record InvitePreviewResponse(string Email, string TenantName, string Role, DateTime ExpiresAt);

/// <summary>Response when a user accepts an invite — includes a JWT for immediate login.</summary>
public sealed record AcceptInviteResponse(string Token, UserDto User);

/// <summary>Response DTO for a tenant member (user).</summary>
public sealed record TenantMemberResponse(Guid Id, string Email, string DisplayName, string Role, DateTime CreatedAt);

/// <summary>Response DTO for tenant configuration.</summary>
public sealed record TenantConfigResponse(
    string? DisplayName,
    string? LogoUrl,
    int MaxConcurrentExecutions,
    int ExecutionTimeoutSeconds,
    string DefaultTimezone,
    bool AllowGuestFormFill
);
