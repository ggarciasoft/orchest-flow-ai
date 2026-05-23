using OrchestAI.Domain.Enums;
namespace OrchestAI.Domain.Entities;
public sealed class WorkflowExecution
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid WorkflowId { get; private set; }
    public Guid WorkflowVersionId { get; private set; }
    public ExecutionStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? TriggeredBy { get; private set; }
    public string InputJson { get; private set; } = "{}";
    public string? OutputJson { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string CorrelationId { get; private set; } = default!;
    private WorkflowExecution() { }
    public static WorkflowExecution Create(Guid tenantId, Guid workflowId, Guid workflowVersionId, Guid? triggeredBy, string inputJson, string correlationId)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, WorkflowId = workflowId, WorkflowVersionId = workflowVersionId, Status = ExecutionStatus.Queued, StartedAt = DateTime.UtcNow, TriggeredBy = triggeredBy, InputJson = inputJson, CorrelationId = correlationId };
    public void Start() => Status = ExecutionStatus.Running;
    public void Complete(string? outputJson) { Status = ExecutionStatus.Completed; OutputJson = outputJson; CompletedAt = DateTime.UtcNow; }
    public void Fail(string errorMessage) { Status = ExecutionStatus.Failed; ErrorMessage = errorMessage; CompletedAt = DateTime.UtcNow; }
    public void Pause() => Status = ExecutionStatus.Paused;
    public void Resume() => Status = ExecutionStatus.Running;
    public void Cancel() { Status = ExecutionStatus.Cancelled; CompletedAt = DateTime.UtcNow; }
}