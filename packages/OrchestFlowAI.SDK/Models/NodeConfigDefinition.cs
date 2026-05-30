namespace OrchestFlowAI.SDK.Models;
public sealed record NodeConfigDefinition(
    string Key, string DisplayName, string Description, DataType Type, bool Required,
    object? DefaultValue = null,
    IReadOnlyCollection<string>? AllowedValues = null,
    string? OptionsSource = null,
    /// <summary>Optional per-value descriptions for Enum fields. Key = allowed value, Value = description shown in the UI.</summary>
    IReadOnlyDictionary<string, string>? OptionDescriptions = null,
    /// <summary>When true, the value is sensitive (API key, password, token). The UI masks the input and suggests using {{secret:name}} references instead of raw values.</summary>
    bool IsSensitive = false,
    /// <summary>When true, the UI renders a multi-line textarea instead of a single-line input. Suitable for SQL queries, JSON, prompts, and other long-form text.</summary>
    bool IsMultiline = false
);