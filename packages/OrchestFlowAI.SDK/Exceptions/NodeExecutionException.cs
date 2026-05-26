namespace OrchestFlowAI.SDK.Exceptions;
public sealed class NodeExecutionException : Exception
{
    public string? Code { get; init; }
    public bool Retryable { get; init; }
    public NodeExecutionException(string message, Exception? inner = null) : base(message, inner) { }
    /// <summary>Creates a node execution exception with an error code and retryable flag.</summary>
    public NodeExecutionException(string code, string message, bool retryable = false, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
        Retryable = retryable;
    }
}