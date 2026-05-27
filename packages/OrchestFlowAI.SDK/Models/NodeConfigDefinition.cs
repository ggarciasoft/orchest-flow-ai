namespace OrchestFlowAI.SDK.Models;
public sealed record NodeConfigDefinition(
    string Key, string DisplayName, string Description, DataType Type, bool Required,
    object? DefaultValue = null,
    IReadOnlyCollection<string>? AllowedValues = null,
    string? OptionsSource = null,
    /// <summary>Optional per-value descriptions for Enum fields. Key = allowed value, Value = description shown in the UI.</summary>
    IReadOnlyDictionary<string, string>? OptionDescriptions = null
);