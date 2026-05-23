using OrchestAI.Engine.Validation;
using FluentAssertions;

namespace OrchestAI.Tests.EngineTests;

public sealed class ValidationResultTests
{
    [Fact]
    public void Success_ShouldBeValid_WithNoErrors()
    {
        var result = ValidationResult.Success();

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_ShouldBeInvalid_WithErrors()
    {
        var errors = new[] { new ValidationError("node-1", "Unknown type") };
        var result = ValidationResult.Failure(errors);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].NodeId.Should().Be("node-1");
        result.Errors[0].Message.Should().Be("Unknown type");
    }

    [Fact]
    public void Failure_WithMultipleErrors_ShouldContainAll()
    {
        var errors = new[]
        {
            new ValidationError("*", "No start node"),
            new ValidationError("*", "No end node")
        };
        var result = ValidationResult.Failure(errors);

        result.Errors.Should().HaveCount(2);
    }
}
