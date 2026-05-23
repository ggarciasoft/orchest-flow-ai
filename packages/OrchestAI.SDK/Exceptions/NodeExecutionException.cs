namespace OrchestAI.SDK.Exceptions;
public sealed class NodeExecutionException : Exception
{
    public string? Code { get; init; }
    public bool Retryable { get; init; }
    public NodeExecutionException(string message, Exception? inner = null) : base(message, inner) { }
}