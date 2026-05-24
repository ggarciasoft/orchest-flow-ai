using FluentAssertions;
using OrchestFlowAI.Domain.Entities;
using System;
using Xunit;

namespace OrchestFlowAI.Tests.DomainTests;

public sealed class AIUsageLogTests
{
    [Fact]
    public void Create_ShouldReturnValidAIUsageLog()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var workflowExecutionId = Guid.NewGuid();
        var nodeExecutionId = Guid.NewGuid();
        var provider = "TestProvider";
        var model = "TestModel";
        var promptVersion = "v1";
        var promptTokens = 100;
        var completionTokens = 150;
        var estimatedCostUsd = 0.05m;

        // Act
        var usageLog = AIUsageLog.Create(
            tenantId,
            workflowExecutionId,
            nodeExecutionId,
            provider,
            model,
            promptVersion,
            promptTokens,
            completionTokens,
            estimatedCostUsd);

        // Assert
        usageLog.Should().NotBeNull();
        usageLog.Id.Should().NotBeEmpty();
        usageLog.TenantId.Should().Be(tenantId);
        usageLog.WorkflowExecutionId.Should().Be(workflowExecutionId);
        usageLog.NodeExecutionId.Should().Be(nodeExecutionId);
        usageLog.Provider.Should().Be(provider);
        usageLog.Model.Should().Be(model);
        usageLog.PromptVersion.Should().Be(promptVersion);
        usageLog.PromptTokens.Should().Be(promptTokens);
        usageLog.CompletionTokens.Should().Be(completionTokens);
        usageLog.TotalTokens.Should().Be(promptTokens + completionTokens);
        usageLog.EstimatedCostUsd.Should().Be(estimatedCostUsd);
        usageLog.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }
}