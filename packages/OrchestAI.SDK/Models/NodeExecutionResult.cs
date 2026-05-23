namespace OrchestAI.SDK.Models;
public sealed class NodeExecutionResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public bool Retryable { get; init; }
    public Dictionary<string, object?> Outputs { get; init; } = new();
    public NodeExecutionStatus Status { get; init; }

    public static NodeExecutionResult Succeeded(Dictionary<string, object?> outputs) =>
        new() { Success = true, Status = NodeExecutionStatus.Succeeded, Outputs = outputs };
    public static NodeExecutionResult Failed(string message, string? code = null, bool retryable = false) =>
        new() { Success = false, Status = NodeExecutionStatus.Failed, ErrorMessage = message, ErrorCode = code, Retryable = retryable };
    public static NodeExecutionResult WaitingForApproval(Dictionary<string, object?> payload) =>
        new() { Success = true, Status = NodeExecutionStatus.WaitingForApproval, Outputs = payload };
    public static NodeExecutionResult Skipped() =>
        new() { Success = true, Status = NodeExecutionStatus.Skipped };
}