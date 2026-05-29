namespace OrchestFlowAI.Domain.ValueObjects;

/// <summary>
/// Per-tenant configuration controlling branding, execution limits, and feature behavior.
/// Stored as JSON on the Tenant row — no extra table required.
/// </summary>
public sealed class TenantConfig
{
    /// <summary>Branded display name shown in the UI (may differ from the internal Tenant.Name).</summary>
    public string? DisplayName { get; set; }

    /// <summary>Optional URL for the tenant's logo image.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Maximum number of workflow executions that may run concurrently for this tenant. 0 = unlimited.</summary>
    public int MaxConcurrentExecutions { get; set; } = 10;

    /// <summary>Maximum wall-clock seconds a single execution may run before being force-cancelled. 0 = unlimited.</summary>
    public int ExecutionTimeoutSeconds { get; set; } = 3600;

    /// <summary>Default IANA timezone used to display cron schedules and execution timestamps in the UI.</summary>
    public string DefaultTimezone { get; set; } = "UTC";

    /// <summary>
    /// When true the form-fill page is publicly accessible without authentication.
    /// When false unauthenticated requests to GET /api/forms/{id}/fill return 401.
    /// </summary>
    public bool AllowGuestFormFill { get; set; } = true;

    /// <summary>Returns a new instance with all default values.</summary>
    public static TenantConfig Default() => new();
}
