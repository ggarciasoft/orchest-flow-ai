using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Testing;
using FluentAssertions;

namespace OrchestFlowAI.Tests.SDKTests;

public sealed class WorkflowExecutionContextTests
{
    [Fact]
    public void GetInput_ExistingStringKey_ShouldReturnValue()
    {
        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["name"] = "Alice" })
            .Build();

        ctx.GetInput<string>("name").Should().Be("Alice");
    }

    [Fact]
    public void GetInput_MissingKey_ShouldReturnDefault()
    {
        var ctx = new TestContextBuilder().Build();
        ctx.GetInput<string>("missing").Should().BeNull();
    }

    [Fact]
    public void GetConfig_ExistingKey_ShouldReturnValue()
    {
        var ctx = new TestContextBuilder()
            .WithConfig(new() { ["model"] = "gpt-4o" })
            .Build();

        ctx.GetConfig<string>("model").Should().Be("gpt-4o");
    }

    [Fact]
    public void GetInput_IntValue_ShouldReturnCast()
    {
        var ctx = new TestContextBuilder()
            .WithInputs(new() { ["count"] = 42 })
            .Build();

        ctx.GetInput<int>("count").Should().Be(42);
    }

    [Fact]
    public void Build_ShouldPopulateExecutionId()
    {
        var id = Guid.NewGuid();
        var ctx = new TestContextBuilder().WithExecutionId(id).Build();
        ctx.ExecutionId.Should().Be(id);
    }
}
