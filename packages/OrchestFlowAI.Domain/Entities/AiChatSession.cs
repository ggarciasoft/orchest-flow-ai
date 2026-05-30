namespace OrchestFlowAI.Domain.Entities;

/// <summary>
/// Represents an interactive AI chat session (workflow designer assist, form generator, etc.).
/// </summary>
public sealed class AiChatSession
{
    public Guid   Id          { get; private set; }
    public Guid   TenantId    { get; private set; }
    public Guid   UserId      { get; private set; }
    /// <summary>Surface that originated the session: "workflow-assist", "form-generator", "node-assist".</summary>
    public string Surface     { get; private set; } = default!;
    /// <summary>Optional context entity id (workflow id, form id, etc.).</summary>
    public Guid?  ContextId   { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private AiChatSession() { }

    public static AiChatSession Create(Guid tenantId, Guid userId, string surface, Guid? contextId = null)
        => new()
        {
            Id        = Guid.NewGuid(),
            TenantId  = tenantId,
            UserId    = userId,
            Surface   = surface,
            ContextId = contextId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    public void Touch() => UpdatedAt = DateTime.UtcNow;
}
