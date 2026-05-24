using Moq;
using OrchestFlowAI.Engine.Registry;
using OrchestFlowAI.SDK.Interfaces;
using FluentAssertions;

namespace OrchestFlowAI.Tests.EngineTests;

public sealed class NodeRegistryTests
{
    [Fact]
    public void Register_AndGetNode_ShouldReturnRegisteredNode()
    {
        var registry = new NodeRegistry();
        var node = new Mock<IWorkflowNode>();
        node.Setup(n => n.Type).Returns("test.node");
        var descriptor = new Mock<IWorkflowNodeDescriptor>();
        descriptor.Setup(d => d.Type).Returns("test.node");

        registry.Register(node.Object, descriptor.Object);

        registry.GetNode("test.node").Should().Be(node.Object);
    }

    [Fact]
    public void Register_AndGetDescriptor_ShouldReturnRegisteredDescriptor()
    {
        var registry = new NodeRegistry();
        var node = new Mock<IWorkflowNode>();
        node.Setup(n => n.Type).Returns("test.node");
        var descriptor = new Mock<IWorkflowNodeDescriptor>();
        descriptor.Setup(d => d.Type).Returns("test.node");

        registry.Register(node.Object, descriptor.Object);

        registry.GetDescriptor("test.node").Should().Be(descriptor.Object);
    }

    [Fact]
    public void GetNode_UnknownType_ShouldReturnNull()
    {
        var registry = new NodeRegistry();
        registry.GetNode("nonexistent").Should().BeNull();
    }

    [Fact]
    public void GetAllDescriptors_ShouldReturnAllRegistered()
    {
        var registry = new NodeRegistry();
        for (var i = 0; i < 3; i++)
        {
            var node = new Mock<IWorkflowNode>();
            node.Setup(n => n.Type).Returns($"type.{i}");
            var desc = new Mock<IWorkflowNodeDescriptor>();
            desc.Setup(d => d.Type).Returns($"type.{i}");
            registry.Register(node.Object, desc.Object);
        }

        registry.GetAllDescriptors().Should().HaveCount(3);
    }
}
