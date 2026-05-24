namespace OrchestFlowAI.Domain.ValueObjects;

/// <summary>
/// Configures automatic retry behavior with exponential backoff for workflow node executions.
/// </summary>
public sealed record RetryPolicy
{
    /// <summary>
    /// Gets the maximum number of attempts. 0 means no retry (fail on first error).
    /// </summary>
    public int MaxAttempts { get; init; }

    /// <summary>
    /// Gets the base delay in milliseconds before the first retry.
    /// </summary>
    public int BackoffMs { get; init; }

    /// <summary>
    /// Gets the multiplier applied to the backoff on each successive attempt.
    /// Defaults to 2.0 (double the delay each retry).
    /// </summary>
    public double BackoffMultiplier { get; init; }

    /// <summary>
    /// Returns a <see cref="RetryPolicy"/> with no retry (MaxAttempts = 0).
    /// </summary>
    public static RetryPolicy None { get; } = new() { MaxAttempts = 0, BackoffMs = 0, BackoffMultiplier = 2.0 };

    /// <summary>
    /// Creates a new <see cref="RetryPolicy"/> with the given parameters.
    /// </summary>
    /// <param name="maxAttempts">Maximum number of attempts (must be &gt;= 1 to retry).</param>
    /// <param name="backoffMs">Base delay in milliseconds.</param>
    /// <param name="multiplier">Backoff multiplier (default 2.0).</param>
    /// <returns>A configured <see cref="RetryPolicy"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxAttempts"/>, <paramref name="backoffMs"/>, or <paramref name="multiplier"/> are out of valid range.
    /// </exception>
    public static RetryPolicy Create(int maxAttempts, int backoffMs, double multiplier = 2.0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxAttempts);
        ArgumentOutOfRangeException.ThrowIfNegative(backoffMs);
        if (multiplier < 1.0)
            throw new ArgumentOutOfRangeException(nameof(multiplier), "BackoffMultiplier must be >= 1.0.");

        return new() { MaxAttempts = maxAttempts, BackoffMs = backoffMs, BackoffMultiplier = multiplier };
    }

    /// <summary>
    /// Calculates the delay before a given retry attempt using exponential backoff.
    /// </summary>
    /// <param name="attemptNumber">The 1-based attempt number (1 = first retry delay).</param>
    /// <returns>The calculated delay as a <see cref="TimeSpan"/>.</returns>
    public TimeSpan GetDelay(int attemptNumber)
    {
        if (attemptNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be >= 1.");

        var ms = BackoffMs * Math.Pow(BackoffMultiplier, attemptNumber - 1);
        return TimeSpan.FromMilliseconds(ms);
    }
}
