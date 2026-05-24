using FluentAssertions;
using OrchestAI.Domain.ValueObjects;

namespace OrchestAI.Tests.DomainTests;

/// <summary>
/// Unit tests for <see cref="RetryPolicy"/> value object.
/// </summary>
public sealed class RetryPolicyTests
{
    [Fact]
    public void None_Should_HaveMaxAttemptsZero()
    {
        RetryPolicy.None.MaxAttempts.Should().Be(0);
        RetryPolicy.None.BackoffMs.Should().Be(0);
        RetryPolicy.None.BackoffMultiplier.Should().Be(2.0);
    }

    [Fact]
    public void Create_Should_ReturnPolicyWithGivenValues()
    {
        var policy = RetryPolicy.Create(3, 500, 2.0);

        policy.MaxAttempts.Should().Be(3);
        policy.BackoffMs.Should().Be(500);
        policy.BackoffMultiplier.Should().Be(2.0);
    }

    [Fact]
    public void Create_WithDefaultMultiplier_ShouldUse2()
    {
        var policy = RetryPolicy.Create(2, 100);

        policy.BackoffMultiplier.Should().Be(2.0);
    }

    [Fact]
    public void Create_WithNegativeMaxAttempts_ShouldThrow()
    {
        var act = () => RetryPolicy.Create(-1, 100);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNegativeBackoffMs_ShouldThrow()
    {
        var act = () => RetryPolicy.Create(3, -100);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithMultiplierBelowOne_ShouldThrow()
    {
        var act = () => RetryPolicy.Create(3, 100, 0.5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetDelay_Attempt1_ShouldReturnBaseDelay()
    {
        // Attempt 1: backoffMs * (multiplier ^ 0) = 500 * 1 = 500ms
        var policy = RetryPolicy.Create(3, 500, 2.0);

        var delay = policy.GetDelay(1);

        delay.Should().Be(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void GetDelay_Attempt2_ShouldDoubleDelay()
    {
        // Attempt 2: 500 * (2 ^ 1) = 1000ms
        var policy = RetryPolicy.Create(3, 500, 2.0);

        var delay = policy.GetDelay(2);

        delay.Should().Be(TimeSpan.FromMilliseconds(1000));
    }

    [Fact]
    public void GetDelay_Attempt3_ShouldQuadrupleBaseDelay()
    {
        // Attempt 3: 500 * (2 ^ 2) = 2000ms
        var policy = RetryPolicy.Create(3, 500, 2.0);

        var delay = policy.GetDelay(3);

        delay.Should().Be(TimeSpan.FromMilliseconds(2000));
    }

    [Fact]
    public void GetDelay_WithAttemptZero_ShouldThrow()
    {
        var policy = RetryPolicy.Create(3, 500);

        var act = () => policy.GetDelay(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetDelay_WithCustomMultiplier_ShouldApplyCorrectly()
    {
        // Multiplier 1.5, base 100ms, attempt 3: 100 * (1.5 ^ 2) = 225ms
        var policy = RetryPolicy.Create(5, 100, 1.5);

        var delay = policy.GetDelay(3);

        delay.TotalMilliseconds.Should().BeApproximately(225, 1);
    }

    [Fact]
    public void None_GetDelay_Attempt1_ShouldReturnZero()
    {
        // None has BackoffMs = 0 so delay is always zero
        var delay = RetryPolicy.None.GetDelay(1);

        delay.Should().Be(TimeSpan.Zero);
    }
}
