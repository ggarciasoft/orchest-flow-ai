namespace OrchestFlowAI.Domain.Entities;
public sealed class AuditLog
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? ActorId { get; private set; }
    public string Action { get; private set; } = default!;
    public string TargetType { get; private set; } = default!;
    public Guid? TargetId { get; private set; }
    public string? PayloadJson { get; private set; }
    public DateTime CreatedAt { get; private set; }
    private AuditLog() { }
    public static AuditLog Create(Guid tenantId, Guid? actorId, string action, string targetType, Guid? targetId, string? payloadJson = null)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, ActorId = actorId, Action = action, TargetType = targetType, TargetId = targetId, PayloadJson = payloadJson, CreatedAt = DateTime.UtcNow };
}