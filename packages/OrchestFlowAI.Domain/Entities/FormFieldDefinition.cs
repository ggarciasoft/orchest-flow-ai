namespace OrchestFlowAI.Domain.Entities;

/// <summary>Value object describing a single field in a custom form definition.</summary>
public sealed record FormFieldDefinition(
    string Key,
    string Label,
    string Type, // text|number|select|date|email|boolean
    bool Required = false,
    string? Placeholder = null,
    string[]? Options = null // for select type
);
