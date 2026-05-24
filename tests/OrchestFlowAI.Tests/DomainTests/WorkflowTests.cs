using OrchestFlowAI.Domain.Entities;
using FluentAssertions;

namespace OrchestFlowAI.Tests.DomainTests;

public class WorkflowTests
{
    [Fact]
    public void Create_ShouldInitializeProperly()
    {
        var tenantId = Guid.NewGuid();
        var name = "Test Workflow";
        var description = "A sample workflow.";
        var createdBy = Guid.NewGuid();

        var workflow = Workflow.Create(tenantId, name, description, createdBy);

        workflow.TenantId.Should().Be(tenantId);
        workflow.Name.Should().Be(name);
        workflow.Description.Should().Be(description);
        workflow.CreatedBy.Should().Be(createdBy);
        workflow.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        workflow.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        workflow.IsDeleted.Should().BeFalse();
        workflow.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Delete_ShouldSetDeletionFields()
    {
        var workflow = Workflow.Create(Guid.NewGuid(), "Name", "Description", Guid.NewGuid());

        workflow.Delete();

        workflow.IsDeleted.Should().BeTrue();
        workflow.DeletedAt.Should().NotBeNull();
        workflow.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Restore_ShouldUnsetDeletionFields()
    {
        var workflow = Workflow.Create(Guid.NewGuid(), "Name", "Description", Guid.NewGuid());
        workflow.Delete();

        workflow.Restore();

        workflow.IsDeleted.Should().BeFalse();
        workflow.DeletedAt.Should().BeNull();
        workflow.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }
}