namespace OrchestAI.SDK.Models;
public sealed record NodeInputDefinition(string Key, string DisplayName, string Description, DataType Type, bool Required, object? DefaultValue = null);