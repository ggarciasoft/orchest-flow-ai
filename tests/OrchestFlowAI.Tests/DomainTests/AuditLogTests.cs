using FluentAssertions;
using OrchestFlowAI.Domain.Entities;
using System;
using Xunit;

namespace OrchestFlowAI.Tests.DomainTests;

public sealed class AuditLogTests
{
    [Fact]
    public void Create_ShouldReturnValidAuditLog()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var action = "CreateWorkflow";
        var targetType = "Workflow";
        var targetId = Guid.NewGuid();
        var payloadJson = "{\"key\":\"value\"}";

        // Act
        var auditLog = AuditLog.Create(tenantId, actorId, action, targetType, targetId, payloadJson);

        // Assert
        auditLog.Should().NotBeNull();
        auditLog.Id.Should().NotBeEmpty();
        auditLog.TenantId.Should().Be(tenantId);
        auditLog.ActorId.Should().Be(actorId);
        auditLog.Action.Should().Be(action);
        auditLog.TargetType.Should().Be(targetType);
        auditLog.TargetId.Should().Be(targetId);
        auditLog.PayloadJson.Should().Be(payloadJson);
        auditLog.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }
}