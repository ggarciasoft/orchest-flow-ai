namespace OrchestFlowAI.Contracts.Requests;

/// <summary>Request body for creating a new tenant workspace.</summary>
public sealed record CreateTenantRequest(string Name);

/// <summary>Request body for inviting a user to a tenant.</summary>
public sealed record InviteUserRequest(string Email, string Role);

/// <summary>Request body for accepting a tenant invite and creating an account.</summary>
public sealed record AcceptInviteRequest(string Token, string Password);

/// <summary>Request body for updating tenant configuration. Null fields are left unchanged.</summary>
public sealed record UpdateTenantConfigRequest(
    string? DisplayName,
    string? LogoUrl,
    int? MaxConcurrentExecutions,
    int? ExecutionTimeoutSeconds,
    string? DefaultTimezone,
    bool? AllowGuestFormFill
);
