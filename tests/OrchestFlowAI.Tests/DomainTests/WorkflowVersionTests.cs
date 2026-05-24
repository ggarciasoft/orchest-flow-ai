using OrchestFlowAI.Domain.Entities;
using FluentAssertions;

namespace OrchestFlowAI.Tests.DomainTests;

public class WorkflowVersionTests
{
    [Fact]
    public void Create_ShouldInitializeProperly()
    {
        var workflowId = Guid.NewGuid();
        var versionNumber = 1;
        var definitionJson = "{\"key\":\"value\"}";
        var createdBy = Guid.NewGuid();

        var version = WorkflowVersion.Create(workflowId, versionNumber, definitionJson, createdBy);

        version.WorkflowId.Should().Be(workflowId);
        version.VersionNumber.Should().Be(versionNumber);
        version.DefinitionJson.Should().Be(definitionJson);
        version.CreatedBy.Should().Be(createdBy);
        version.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        version.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        var version = WorkflowVersion.Create(Guid.NewGuid(), 1, "{}", Guid.NewGuid());

        version.Activate();

        version.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        var version = WorkflowVersion.Create(Guid.NewGuid(), 1, "{}", Guid.NewGuid());

        version.Activate();
        version.Deactivate();

        version.IsActive.Should().BeFalse();
    }
}