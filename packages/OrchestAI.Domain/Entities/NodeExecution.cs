using OrchestAI.Domain.Enums;
namespace OrchestAI.Domain.Entities;
public sealed class NodeExecution
{
    public Guid Id { get; private set; }
    public Guid WorkflowExecutionId { get; private set; }
    public string NodeId { get; private set; } = default!;
    public string NodeType { get; private set; } = default!;
    public NodeExecutionStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? InputJson { get; private set; }
    public string? OutputJson { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public int Step { get; private set; }
    private NodeExecution() { }
    public static NodeExecution Create(Guid workflowExecutionId, string nodeId, string nodeType, int step)
        => new() { Id = Guid.NewGuid(), WorkflowExecutionId = workflowExecutionId, NodeId = nodeId, NodeType = nodeType, Status = NodeExecutionStatus.Pending, Step = step };
    public void Start(string? inputJson) { Status = NodeExecutionStatus.Running; StartedAt = DateTime.UtcNow; InputJson = inputJson; }
    public void Succeed(string outputJson) { Status = NodeExecutionStatus.Succeeded; OutputJson = outputJson; CompletedAt = DateTime.UtcNow; }
    public void Fail(string errorMessage) { Status = NodeExecutionStatus.Failed; ErrorMessage = errorMessage; CompletedAt = DateTime.UtcNow; RetryCount++; }
    public void WaitForApproval(string payloadJson) { Status = NodeExecutionStatus.WaitingForApproval; OutputJson = payloadJson; }
    public void Skip() { Status = NodeExecutionStatus.Skipped; CompletedAt = DateTime.UtcNow; }
    public void Cancel() { Status = NodeExecutionStatus.Cancelled; CompletedAt = DateTime.UtcNow; }
}