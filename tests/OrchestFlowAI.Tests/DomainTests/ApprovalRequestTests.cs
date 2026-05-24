using FluentAssertions;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
using System;
using Xunit;

namespace OrchestFlowAI.Tests.DomainTests;

public sealed class ApprovalRequestTests
{
    [Fact]
    public void Create_ShouldReturnValidApprovalRequest()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var workflowExecutionId = Guid.NewGuid();
        var nodeExecutionId = Guid.NewGuid();
        var payloadJson = "{\"key\":\"value\"}";
        var requestedBy = Guid.NewGuid();
        var slaMinutes = 60;

        // Act
        var approvalRequest = ApprovalRequest.Create(
            tenantId,
            workflowExecutionId,
            nodeExecutionId,
            payloadJson,
            requestedBy,
            slaMinutes);

        // Assert
        approvalRequest.Should().NotBeNull();
        approvalRequest.Id.Should().NotBeEmpty();
        approvalRequest.TenantId.Should().Be(tenantId);
        approvalRequest.WorkflowExecutionId.Should().Be(workflowExecutionId);
        approvalRequest.NodeExecutionId.Should().Be(nodeExecutionId);
        approvalRequest.Status.Should().Be(ApprovalStatus.Pending);
        approvalRequest.PayloadJson.Should().Be(payloadJson);
        approvalRequest.RequestedBy.Should().Be(requestedBy);
        approvalRequest.SlaMinutes.Should().Be(slaMinutes);
        approvalRequest.RequestedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Approve_ShouldSetCorrectProperties()
    {
        // Arrange
        var approvalRequest = ApprovalRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "{}",
            Guid.NewGuid());
        var respondedBy = Guid.NewGuid();
        var comment = "Approved for execution.";

        // Act
        approvalRequest.Approve(respondedBy, comment);

        // Assert
        approvalRequest.Status.Should().Be(ApprovalStatus.Approved);
        approvalRequest.RespondedBy.Should().Be(respondedBy);
        approvalRequest.Decision.Should().Be("approved");
        approvalRequest.Comment.Should().Be(comment);
        approvalRequest.RespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Reject_ShouldSetCorrectProperties()
    {
        // Arrange
        var approvalRequest = ApprovalRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "{}",
            Guid.NewGuid());
        var respondedBy = Guid.NewGuid();
        var comment = "Rejected due to missing information.";

        // Act
        approvalRequest.Reject(respondedBy, comment);

        // Assert
        approvalRequest.Status.Should().Be(ApprovalStatus.Rejected);
        approvalRequest.RespondedBy.Should().Be(respondedBy);
        approvalRequest.Decision.Should().Be("rejected");
        approvalRequest.Comment.Should().Be(comment);
        approvalRequest.RespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Expire_ShouldSetStatusToExpired()
    {
        // Arrange
        var approvalRequest = ApprovalRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "{}",
            Guid.NewGuid());

        // Act
        approvalRequest.Expire();

        // Assert
        approvalRequest.Status.Should().Be(ApprovalStatus.Expired);
        approvalRequest.RespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }
}