namespace OrchestFlowAI.Application.Abstractions;

/// <summary>
/// Sends transactional emails (invites, welcome, notifications).
/// Inject this abstraction in controllers/services; swap the implementation via DI.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email to a single recipient.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="htmlBody">HTML version of the body.</param>
    /// <param name="textBody">Optional plain-text fallback body.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default);
}
