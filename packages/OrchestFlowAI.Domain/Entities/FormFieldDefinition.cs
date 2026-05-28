namespace OrchestFlowAI.Domain.Entities;

/// <summary>Value object describing a single field in a custom form definition.</summary>
public sealed record FormFieldDefinition(
    string Key,
    string Label,
    string Type, // text|number|select|date|email|boolean
    bool Required = false,
    string? Placeholder = null,
    string[]? Options = null, // for select type: static options list
    /// <summary>
    /// For select fields: the output key from a previous node whose value is a JSON array of strings.
    /// Resolved at fill-page load time by reading the execution's node outputs.
    /// Example: if a db-query node outputs rows=["Food","Transport","Other"], set optionsFrom="rows".
    /// </summary>
    string? OptionsFrom = null
);
