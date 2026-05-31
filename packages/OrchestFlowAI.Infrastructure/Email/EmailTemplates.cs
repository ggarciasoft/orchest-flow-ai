namespace OrchestFlowAI.Infrastructure.Email;

/// <summary>
/// Builds transactional email bodies for invite and welcome flows.
/// HTML templates are intentionally minimal — swap for a proper template engine when adding branding.
/// </summary>
public static class EmailTemplates
{
    // ── Invite ─────────────────────────────────────────────────────────────

    public static string InviteSubject(string workspaceName)
        => $"You've been invited to join {workspaceName} on OrchestFlowAI";

    public static string InviteHtml(string workspaceName, string role, string acceptUrl, DateTime expiresAt)
        => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:sans-serif;color:#1e293b;max-width:520px;margin:0 auto;padding:32px 16px">
          <h2 style="margin-bottom:8px">You've been invited 🎉</h2>
          <p style="color:#475569">
            You've been invited to join <strong>{HtmlEncode(workspaceName)}</strong> on OrchestFlowAI
            with the <strong>{HtmlEncode(role)}</strong> role.
          </p>
          <p style="margin:24px 0">
            <a href="{acceptUrl}"
               style="background:#4f46e5;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:600;display:inline-block">
              Accept Invitation
            </a>
          </p>
          <p style="color:#94a3b8;font-size:13px">
            This link expires on {expiresAt:dddd, MMMM d} at {expiresAt:HH:mm} UTC.
            If you were not expecting this invitation, you can safely ignore this email.
          </p>
          <hr style="border:none;border-top:1px solid #e2e8f0;margin:24px 0" />
          <p style="color:#cbd5e1;font-size:12px">OrchestFlowAI — AI Workflow Platform</p>
        </body>
        </html>
        """;

    public static string InviteText(string workspaceName, string role, string acceptUrl, DateTime expiresAt)
        => $"""
        You've been invited to join {workspaceName} on OrchestFlowAI with the {role} role.

        Accept your invitation here:
        {acceptUrl}

        This link expires on {expiresAt:yyyy-MM-dd HH:mm} UTC.

        If you were not expecting this invitation, you can safely ignore this email.
        """;

    // ── Welcome ────────────────────────────────────────────────────────────

    public static string WelcomeSubject() => "Welcome to OrchestFlowAI!";

    public static string WelcomeHtml(string displayName, string loginUrl)
        => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:sans-serif;color:#1e293b;max-width:520px;margin:0 auto;padding:32px 16px">
          <h2 style="margin-bottom:8px">Welcome to OrchestFlowAI, {HtmlEncode(displayName)}!</h2>
          <p style="color:#475569">
            Your account has been created. You are the <strong>Admin</strong> of your new workspace
            and can start building AI workflows straight away.
          </p>
          <p style="margin:24px 0">
            <a href="{loginUrl}"
               style="background:#4f46e5;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:600;display:inline-block">
              Go to Dashboard
            </a>
          </p>
          <p style="color:#94a3b8;font-size:13px">
            Need help? Check the <a href="{loginUrl}/docs" style="color:#6366f1">documentation</a>.
          </p>
          <hr style="border:none;border-top:1px solid #e2e8f0;margin:24px 0" />
          <p style="color:#cbd5e1;font-size:12px">OrchestFlowAI — AI Workflow Platform</p>
        </body>
        </html>
        """;

    public static string WelcomeText(string displayName, string loginUrl)
        => $"""
        Welcome to OrchestFlowAI, {displayName}!

        Your account has been created. You are the Admin of your new workspace.

        Log in here: {loginUrl}

        Need help? Visit {loginUrl}/docs
        """;

    // ── Helpers ────────────────────────────────────────────────────────────

    private static string HtmlEncode(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
