using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrchestFlowAI.Application.Abstractions;

namespace OrchestFlowAI.Infrastructure.Email;

/// <summary>
/// SMTP implementation of <see cref="IEmailService"/>.
/// Configure via appsettings:
/// <code>
/// "Email": {
///   "FromAddress": "noreply@example.com",
///   "FromName":    "OrchestFlowAI",
///   "Smtp": {
///     "Host": "smtp.example.com",
///     "Port": 587,
///     "Username": "user",
///     "Password": "pass",
///     "UseSsl":  true
///   }
/// }
/// </code>
/// Or via env vars: Email__FromAddress, Email__Smtp__Host, Email__Smtp__Port, etc.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
    {
        var host     = _config["Email:Smtp:Host"] ?? throw new InvalidOperationException("Email:Smtp:Host is not configured.");
        var port     = int.Parse(_config["Email:Smtp:Port"] ?? "587");
        var username = _config["Email:Smtp:Username"] ?? "";
        var password = _config["Email:Smtp:Password"] ?? "";
        var useSsl   = bool.Parse(_config["Email:Smtp:UseSsl"] ?? "true");
        var from     = _config["Email:FromAddress"] ?? "noreply@orchestflowai.com";
        var fromName = _config["Email:FromName"]    ?? "OrchestFlowAI";

        using var client = new SmtpClient(host, port)
        {
            EnableSsl   = useSsl,
            Credentials = string.IsNullOrEmpty(username) ? null : new NetworkCredential(username, password),
        };

        using var msg = new MailMessage
        {
            From       = new MailAddress(from, fromName),
            Subject    = subject,
            IsBodyHtml = true,
            Body       = htmlBody,
        };
        msg.To.Add(to);

        if (!string.IsNullOrEmpty(textBody))
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain"));

        _logger.LogInformation("Sending email to {To} subject '{Subject}' via {Host}:{Port}", to, subject, host, port);
        await client.SendMailAsync(msg, ct);
        _logger.LogInformation("Email sent to {To}", to);
    }
}
