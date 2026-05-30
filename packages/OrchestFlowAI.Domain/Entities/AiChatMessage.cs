namespace OrchestFlowAI.Domain.Entities;

/// <summary>
/// A single turn in an <see cref="AiChatSession"/>: user, assistant, or tool call/result.
/// </summary>
public sealed class AiChatMessage
{
    public Guid    Id               { get; private set; }
    public Guid    SessionId        { get; private set; }
    /// <summary>"user" | "assistant" | "tool"</summary>
    public string  Role             { get; private set; } = default!;
    public string? ContentText      { get; private set; }
    /// <summary>Set when Role == "tool": the tool/function name called.</summary>
    public string? ToolName         { get; private set; }
    /// <summary>JSON of tool call arguments.</summary>
    public string? ToolInputJson    { get; private set; }
    /// <summary>JSON of tool result.</summary>
    public string? ToolOutputJson   { get; private set; }
    public int     PromptTokens     { get; private set; }
    public int     CompletionTokens { get; private set; }
    public int     TotalTokens      { get; private set; }
    public string? Model            { get; private set; }
    public string? Provider         { get; private set; }
    public DateTime CreatedAt       { get; private set; }

    private AiChatMessage() { }

    public static AiChatMessage CreateUserMessage(Guid sessionId, string content)
        => new()
        {
            Id          = Guid.NewGuid(),
            SessionId   = sessionId,
            Role        = "user",
            ContentText = content,
            CreatedAt   = DateTime.UtcNow,
        };

    public static AiChatMessage CreateAssistantMessage(
        Guid   sessionId,
        string content,
        string provider,
        string model,
        int    promptTokens,
        int    completionTokens)
        => new()
        {
            Id               = Guid.NewGuid(),
            SessionId        = sessionId,
            Role             = "assistant",
            ContentText      = content,
            Provider         = provider,
            Model            = model,
            PromptTokens     = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens      = promptTokens + completionTokens,
            CreatedAt        = DateTime.UtcNow,
        };

    public static AiChatMessage CreateToolCall(
        Guid   sessionId,
        string toolName,
        string toolInputJson,
        string toolOutputJson,
        string provider,
        string model)
        => new()
        {
            Id              = Guid.NewGuid(),
            SessionId       = sessionId,
            Role            = "tool",
            ToolName        = toolName,
            ToolInputJson   = toolInputJson,
            ToolOutputJson  = toolOutputJson,
            Provider        = provider,
            Model           = model,
            CreatedAt       = DateTime.UtcNow,
        };
}
