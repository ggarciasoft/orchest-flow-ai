using Microsoft.Extensions.Logging;
using OrchestFlowAI.Application.Abstractions;

namespace OrchestFlowAI.Infrastructure.Email;

/// <summary>
/// Development / no-op email service that logs the email instead of sending it.
/// Used automatically when <c>Email:Smtp:Host</c> is not configured.
/// </summary>
public sealed class LogEmailService : IEmailService
{
    private readonly ILogger<LogEmailService> _logger;

    public LogEmailService(ILogger<LogEmailService> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[DEV EMAIL] To={To} | Subject={Subject}\n{Body}",
            to, subject, textBody ?? htmlBody);
        return Task.CompletedTask;
    }
}
