using OrchestAI.SDK.Testing;
using FluentAssertions;

namespace OrchestAI.Tests.SDKTests;

public sealed class TestContextBuilderTests
{
    [Fact]
    public void Build_Default_ShouldHaveNonEmptyIds()
    {
        var ctx = new TestContextBuilder().Build();
        ctx.ExecutionId.Should().NotBeEmpty();
        ctx.TenantId.Should().NotBeEmpty();
        ctx.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WithInputs_ShouldBeAvailableOnContext()
    {
        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["key"] = "value" })
            .Build();
        ctx.NodeInputs["key"].Should().Be("value");
    }

    [Fact]
    public void WithConfig_ShouldBeAvailableOnContext()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["threshold"] = 0.8 })
            .Build();
        ctx.NodeConfig["threshold"].Should().Be(0.8);
    }

    [Fact]
    public void WithWorkflowInputs_ShouldBeAvailableOnContext()
    {
        var ctx = new TestContextBuilder()
            .WithWorkflowInputs(new() { ["documentId"] = "doc-1" })
            .Build();
        ctx.WorkflowInputs["documentId"].Should().Be("doc-1");
    }

    [Fact]
    public void Services_ShouldReturnNullForUnknownType()
    {
        var ctx = new TestContextBuilder().Build();
        ctx.Services.GetService(typeof(string)).Should().BeNull();
    }
}
