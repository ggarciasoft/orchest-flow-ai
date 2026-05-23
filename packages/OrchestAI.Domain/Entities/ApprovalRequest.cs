using OrchestAI.Domain.Enums;
namespace OrchestAI.Domain.Entities;
public sealed class ApprovalRequest
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid WorkflowExecutionId { get; private set; }
    public Guid NodeExecutionId { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public string PayloadJson { get; private set; } = "{}";
    public string? AssigneesJson { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public Guid? RequestedBy { get; private set; }
    public Guid? RespondedBy { get; private set; }
    public string? Decision { get; private set; }
    public string? Comment { get; private set; }
    public int? SlaMinutes { get; private set; }
    private ApprovalRequest() { }
    public static ApprovalRequest Create(Guid tenantId, Guid workflowExecutionId, Guid nodeExecutionId, string payloadJson, Guid? requestedBy, int? slaMinutes = null, string? assigneesJson = null)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, WorkflowExecutionId = workflowExecutionId, NodeExecutionId = nodeExecutionId, Status = ApprovalStatus.Pending, PayloadJson = payloadJson, RequestedBy = requestedBy, SlaMinutes = slaMinutes, AssigneesJson = assigneesJson, RequestedAt = DateTime.UtcNow };
    public void Approve(Guid respondedBy, string? comment) { Status = ApprovalStatus.Approved; RespondedBy = respondedBy; Decision = "approved"; Comment = comment; RespondedAt = DateTime.UtcNow; }
    public void Reject(Guid respondedBy, string? comment) { Status = ApprovalStatus.Rejected; RespondedBy = respondedBy; Decision = "rejected"; Comment = comment; RespondedAt = DateTime.UtcNow; }
    public void Expire() { Status = ApprovalStatus.Expired; RespondedAt = DateTime.UtcNow; }
}