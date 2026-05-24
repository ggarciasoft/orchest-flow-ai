using Moq;
using OrchestFlowAI.Engine.Models;
using OrchestFlowAI.Engine.Registry;
using OrchestFlowAI.Engine.Validation;
using OrchestFlowAI.SDK.Interfaces;
using FluentAssertions;
using System.Text.Json;

namespace OrchestFlowAI.Tests.EngineTests;

public sealed class WorkflowValidatorTests
{
    private readonly WorkflowValidator _validator = new();

    private INodeRegistry BuildRegistry(params string[] types)
    {
        var mock = new Mock<INodeRegistry>();
        foreach (var t in types)
        {
            var desc = new Mock<IWorkflowNodeDescriptor>();
            desc.Setup(d => d.Type).Returns(t);
            mock.Setup(r => r.GetDescriptor(t)).Returns(desc.Object);
        }
        mock.Setup(r => r.GetDescriptor(It.IsNotIn(types))).Returns((IWorkflowNodeDescriptor?)null);
        return mock.Object;
    }

    [Fact]
    public void Validate_ValidWorkflow_ShouldReturnSuccess()
    {
        var def = new WorkflowDefinition
        {
            Nodes = new List<WorkflowNodeDefinition>
            {
                new() { Id = "n1", Type = "system.start" },
                new() { Id = "n2", Type = "system.end" }
            },
            Edges = new List<WorkflowEdgeDefinition> { new() { Source = "n1", Target = "n2" } }
        };
        var registry = BuildRegistry("system.start", "system.end");

        var result = _validator.Validate(def, registry);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingStartNode_ShouldReturnError()
    {
        var def = new WorkflowDefinition
        {
            Nodes = new List<WorkflowNodeDefinition> { new() { Id = "n1", Type = "system.end" } },
            Edges = new List<WorkflowEdgeDefinition>()
        };
        var registry = BuildRegistry("system.end");

        var result = _validator.Validate(def, registry);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("system.start"));
    }

    [Fact]
    public void Validate_UnknownNodeType_ShouldReturnError()
    {
        var def = new WorkflowDefinition
        {
            Nodes = new List<WorkflowNodeDefinition>
            {
                new() { Id = "n1", Type = "system.start" },
                new() { Id = "n2", Type = "unknown.type" },
                new() { Id = "n3", Type = "system.end" }
            },
            Edges = new List<WorkflowEdgeDefinition>()
        };
        var registry = BuildRegistry("system.start", "system.end");

        var result = _validator.Validate(def, registry);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("unknown.type"));
    }

    [Fact]
    public void Validate_InvalidEdgeSource_ShouldReturnError()
    {
        var def = new WorkflowDefinition
        {
            Nodes = new List<WorkflowNodeDefinition>
            {
                new() { Id = "n1", Type = "system.start" },
                new() { Id = "n2", Type = "system.end" }
            },
            Edges = new List<WorkflowEdgeDefinition> { new() { Source = "missing", Target = "n2" } }
        };
        var registry = BuildRegistry("system.start", "system.end");

        var result = _validator.Validate(def, registry);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("missing"));
    }
}
