namespace OrchestFlowAI.Domain.Entities;

/// <summary>
/// Represents user-submitted feedback captured through the feedback form.
/// </summary>
public sealed class Feedback
{
    /// <summary>Gets the unique identifier for this feedback entry.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the tenant that this feedback belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the user who submitted the feedback, if authenticated.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Gets the feedback message text.</summary>
    public string Message { get; private set; } = default!;

    /// <summary>Gets the optional satisfaction rating (1–5).</summary>
    public int? Rating { get; private set; }

    /// <summary>Gets the UTC timestamp when this feedback was created.</summary>
    public DateTime CreatedAt { get; private set; }

    private Feedback() { }

    /// <summary>
    /// Creates a new <see cref="Feedback"/> entry.
    /// </summary>
    /// <param name="tenantId">The tenant submitting feedback.</param>
    /// <param name="message">The feedback message text.</param>
    /// <param name="userId">Optional authenticated user id.</param>
    /// <param name="rating">Optional satisfaction rating (1–5).</param>
    public static Feedback Create(Guid tenantId, string message, Guid? userId = null, int? rating = null)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Message = message,
            Rating = rating,
            CreatedAt = DateTime.UtcNow,
        };
}
