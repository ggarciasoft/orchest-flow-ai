using OrchestFlowAI.Engine.Conditions;
using FluentAssertions;

namespace OrchestFlowAI.Tests.EngineTests;

public sealed class ExpressionEvaluatorTests
{
    private readonly ExpressionEvaluator _evaluator = new();

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void Evaluate_Literal_ShouldReturnCorrectBool(string expr, bool expected)
    {
        var result = _evaluator.Evaluate(expr, new());
        result.Should().Be(expected);
    }

    [Fact]
    public void Evaluate_Equality_WithMatchingValues_ShouldReturnTrue()
    {
        var scope = new Dictionary<string, object?> { ["status"] = "approved" };
        _evaluator.Evaluate("status == 'approved'", scope).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Equality_WithNonMatchingValues_ShouldReturnFalse()
    {
        var scope = new Dictionary<string, object?> { ["status"] = "pending" };
        _evaluator.Evaluate("status == 'approved'", scope).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_Inequality_ShouldWork()
    {
        var scope = new Dictionary<string, object?> { ["x"] = "a" };
        _evaluator.Evaluate("x != 'b'", scope).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_AndOperator_BothTrue_ShouldReturnTrue()
    {
        var scope = new Dictionary<string, object?> { ["a"] = "1", ["b"] = "1" };
        _evaluator.Evaluate("a == '1' && b == '1'", scope).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_OrOperator_OneTrue_ShouldReturnTrue()
    {
        var scope = new Dictionary<string, object?> { ["a"] = "x", ["b"] = "y" };
        _evaluator.Evaluate("a == 'x' || b == 'z'", scope).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_NotOperator_ShouldNegate()
    {
        _evaluator.Evaluate("!false", new()).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_NumericComparison_ShouldWork()
    {
        var scope = new Dictionary<string, object?> { ["score"] = 85.0 };
        _evaluator.Evaluate("score > 80", scope).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_MissingVariable_ShouldReturnFalse()
    {
        _evaluator.Evaluate("missing == 'value'", new()).Should().BeFalse();
    }
}
