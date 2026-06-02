using System.Net.Mail;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.Integrations;

/// <summary>
/// Sends an email via SMTP using per-node configuration.
/// smtpUsername / smtpPassword support {{secret:name}} placeholders resolved by the engine before execution.
/// Subject and body support {{input_key}} substitution from node inputs.
/// </summary>
public sealed class SendEmailNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "integrations.email";

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var to       = ctx.GetConfig<string>("to")      ?? throw new InvalidOperationException("'to' config is required");
        var subject  = ResolvePlaceholders(ctx.GetConfig<string>("subject") ?? throw new InvalidOperationException("'subject' config is required"), ctx.NodeInputs);
        var body     = ResolvePlaceholders(ctx.GetConfig<string>("body")    ?? throw new InvalidOperationException("'body' config is required"),    ctx.NodeInputs);
        // Resolve placeholders in 'to' then split by comma for multiple recipients
        var toResolved = ResolvePlaceholders(to, ctx.NodeInputs);
        var recipients = toResolved.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var host     = ctx.GetConfig<string>("smtpHost")     ?? "localhost";
        var port     = (int)(ctx.GetConfig<double?>("smtpPort") ?? 587.0);
        var username = ctx.GetConfig<string>("smtpUsername") ?? "";
        var password = ctx.GetConfig<string>("smtpPassword") ?? "";
        var useSsl   = ctx.GetConfig<bool?>("smtpUseSsl") ?? (port != 25);

        try
        {
#pragma warning disable SYSLIB0006
            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl   = useSsl,
                Credentials = string.IsNullOrEmpty(username)
                    ? null
                    : new global::System.Net.NetworkCredential(username, password),
            };
            using var mail = new MailMessage
            {
                Subject = subject,
                Body    = body,
                From    = string.IsNullOrEmpty(username) ? new MailAddress("noreply@orchestflowai.com") : new MailAddress(username),
            };
            foreach (var recipient in recipients) mail.To.Add(recipient);
            await smtp.SendMailAsync(mail, ct);
#pragma warning restore SYSLIB0006

            return NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["sent"] = true, ["to"] = string.Join(", ", recipients) });
        }
        catch (Exception ex)
        {
            return NodeExecutionResult.Failed($"SMTP error: {ex.Message}", retryable: true);
        }
    }

    private static string ResolvePlaceholders(string template, IReadOnlyDictionary<string, object?> vars)
    {
        foreach (var kv in vars)
            template = template.Replace($"{{{{{kv.Key}}}}}", kv.Value?.ToString() ?? string.Empty);
        return template;
    }
}

/// <summary>Descriptor for <see cref="SendEmailNode"/>.</summary>
public sealed class SendEmailNodeDescriptor : IWorkflowNodeDescriptor
{
    public string Type        => "integrations.email";
    public string DisplayName => "Send Email";
    public string Description => "Sends an email via SMTP. Credentials support {{secret:name}} substitution.";
    public string Category    => "integrations";
    public string Version     => "1.0.0";
    public string? IconKey    => "mail";

    public IReadOnlyCollection<NodeInputDefinition>  Inputs    => Array.Empty<NodeInputDefinition>();
    public IReadOnlyCollection<NodeOutputDefinition> Outputs   => new[]
    {
        new NodeOutputDefinition("sent", "Sent",      "True if email was sent successfully.", DataType.Boolean),
        new NodeOutputDefinition("to",   "Recipient", "The recipient address.",               DataType.String),
    };
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("to",           "To",            "Recipient(s). Comma-separated. Supports {{placeholders}}.", DataType.String,  Required: true),
        new NodeConfigDefinition("subject",       "Subject",       "Email subject (supports {{placeholders}}).",    DataType.String,  Required: true),
        new NodeConfigDefinition("body",          "Body",          "Email body (supports {{placeholders}}).",       DataType.String,  Required: true,  IsMultiline: true),
        new NodeConfigDefinition("smtpHost",      "SMTP Host",     "SMTP server hostname.",                         DataType.String,  Required: true,  DefaultValue: "smtp.gmail.com"),
        new NodeConfigDefinition("smtpPort",      "SMTP Port",     "SMTP port (587 = STARTTLS, 465 = SSL).",        DataType.Number,  Required: false, DefaultValue: 587),
        new NodeConfigDefinition("smtpUsername",  "SMTP Username", "SMTP login. Use {{secret:my-secret}}.",         DataType.String,  Required: false, IsSensitive: true),
        new NodeConfigDefinition("smtpPassword",  "SMTP Password", "SMTP password. Use {{secret:my-secret}}.",      DataType.String,  Required: false, IsSensitive: true),
        new NodeConfigDefinition("smtpUseSsl",    "Use SSL/TLS",   "Enable SSL/TLS (auto-detected from port).",     DataType.Boolean, Required: false, DefaultValue: true),
    };
}
