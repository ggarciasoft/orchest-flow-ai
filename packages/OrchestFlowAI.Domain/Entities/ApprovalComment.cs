namespace OrchestFlowAI.Domain.Entities;

/// <summary>
/// A comment posted on an approval request — forms a discussion thread
/// visible to all parties while the approval is pending.
/// </summary>
public sealed class ApprovalComment
{
    public Guid Id { get; private set; }
    public Guid ApprovalRequestId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string AuthorName { get; private set; } = default!;
    public string Text { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private ApprovalComment() { }

    public static ApprovalComment Create(Guid approvalRequestId, Guid authorId, string authorName, string text) => new()
    {
        Id = Guid.NewGuid(),
        ApprovalRequestId = approvalRequestId,
        AuthorId = authorId,
        AuthorName = authorName,
        Text = text.Trim(),
        CreatedAt = DateTime.UtcNow,
    };
}
