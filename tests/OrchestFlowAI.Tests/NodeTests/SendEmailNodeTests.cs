using FluentAssertions;
using OrchestFlowAI.Nodes.Integrations;
using OrchestFlowAI.SDK.Models;
using OrchestFlowAI.SDK.Testing;

namespace OrchestFlowAI.Tests.NodeTests;

/// <summary>Unit tests for <see cref="SendEmailNode"/>.</summary>
public sealed class SendEmailNodeTests
{
    [Fact]
    public async Task Execute_InvalidSmtpHost_ShouldReturnFailedResult()
    {
        // Uses a non-existent SMTP host — should return Failed (not throw)
        var ctx = new TestContextBuilder()
            .WithConfig(new()
            {
                ["to"] = "test@example.com",
                ["subject"] = "Test Subject",
                ["body"] = "Hello {{name}}",
                ["smtpHost"] = "invalid.nonexistent.host",
                ["smtpPort"] = "9999"
            })
            .WithInputs(new() { ["name"] = "Alice" })
            .Build();

        var result = await new SendEmailNode().ExecuteAsync(ctx, CancellationToken.None);

        // On SMTP failure, node returns Failed (retryable) rather than throwing
        result.Status.Should().Be(NodeExecutionStatus.Failed);
        result.Retryable.Should().BeTrue();
    }

    [Fact]
    public async Task Execute_MissingTo_ShouldThrow()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["subject"] = "S", ["body"] = "B" })
            .Build();

        var act = async () => await new SendEmailNode().ExecuteAsync(ctx, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Type_ShouldBeIntegrationsEmail()
    {
        new SendEmailNode().Type.Should().Be("integrations.email");
    }
}
