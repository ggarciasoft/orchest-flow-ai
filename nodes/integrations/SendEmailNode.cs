using System.Net.Mail;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;

namespace OrchestFlowAI.Nodes.Integrations;

/// <summary>
/// Sends an email via SMTP with subject and body supporting {{placeholder}} substitution from node inputs.
/// On SMTP failure, returns a retryable Failed result rather than throwing.
/// </summary>
public sealed class SendEmailNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "integrations.email";

    /// <summary>
    /// Resolves placeholders, constructs the email, and sends via SMTP.
    /// </summary>
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var to = ctx.GetConfig<string>("to") ?? throw new InvalidOperationException("to config is required");
        var subject = ResolvePlaceholders(ctx.GetConfig<string>("subject") ?? throw new InvalidOperationException("subject config is required"), ctx.NodeInputs);
        var body = ResolvePlaceholders(ctx.GetConfig<string>("body") ?? throw new InvalidOperationException("body config is required"), ctx.NodeInputs);
        var smtpHost = ctx.GetConfig<string>("smtpHost") ?? "localhost";
        var smtpPort = (int)(ctx.GetConfig<double?>("smtpPort") ?? 25.0);

        try
        {
#pragma warning disable SYSLIB0006 // SmtpClient is deprecated but acceptable for MVP
            using var smtp = new SmtpClient(smtpHost, smtpPort);
            var mail = new MailMessage { Subject = subject, Body = body };
            mail.To.Add(to);
            await smtp.SendMailAsync(mail, ct);
#pragma warning restore SYSLIB0006
            return NodeExecutionResult.Succeeded(new Dictionary<string, object?> { ["sent"] = true, ["to"] = to });
        }
        catch (Exception ex)
        {
            // Retryable = true so the engine can retry on transient SMTP failures
            return NodeExecutionResult.Failed($"SMTP error: {ex.Message}", retryable: true);
        }
    }

    /// <summary>Replaces {{key}} tokens in a template string with values from the given dictionary.</summary>
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
    /// <inheritdoc />
    public string Type => "integrations.email";
    /// <inheritdoc />
    public string DisplayName => "Send Email";
    /// <inheritdoc />
    public string Description => "Sends an email via SMTP. Subject and body support {{placeholder}} substitution.";
    /// <inheritdoc />
    public string Category => "integrations";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "mail";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("sent", "Sent", "True if email was sent successfully.", DataType.Boolean),
        new NodeOutputDefinition("to", "Recipient", "The recipient address.", DataType.String)
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("to", "To", "Recipient email address.", DataType.String, Required: true),
        new NodeConfigDefinition("subject", "Subject", "Email subject (supports {{placeholders}}).", DataType.String, Required: true),
        new NodeConfigDefinition("body", "Body", "Email body (supports {{placeholders}}).", DataType.String, Required: true, IsMultiline: true),
        new NodeConfigDefinition("smtpHost", "SMTP Host", "SMTP server hostname.", DataType.String, Required: false, DefaultValue: "localhost"),
        new NodeConfigDefinition("smtpPort", "SMTP Port", "SMTP port number.", DataType.Number, Required: false, DefaultValue: 25)
    };
}
