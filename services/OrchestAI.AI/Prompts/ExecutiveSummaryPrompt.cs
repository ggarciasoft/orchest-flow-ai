namespace OrchestAI.AI.Prompts;
public static class ExecutiveSummaryPrompt
{
    public const string Version = "2026.05.0";
    public const string System = "You are a professional business writer creating executive summaries.";
    public static string User(string text, int maxWords, string tone) => $"Write a {tone} executive summary of the following content in no more than {maxWords} words.\n\n<content>\n{text}\n</content>\n\nReturn ONLY the summary text.";
}
