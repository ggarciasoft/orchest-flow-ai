namespace OrchestFlowAI.Domain.Entities;

/// <summary>Value object describing a single field in a custom form definition.</summary>
public sealed record FormFieldDefinition(
    string Key,
    string Label,
    string Type, // text|number|select|date|email|boolean|file
    bool Required = false,
    string? Placeholder = null,
    string[]? Options = null, // for select type: static options list
    /// <summary>
    /// For select fields: the output key from a previous node whose value is a JSON array of strings.
    /// Resolved at fill-page load time by reading the execution's node outputs.
    /// Example: if a db-query node outputs rows=["Food","Transport","Other"], set optionsFrom="rows".
    /// </summary>
    string? OptionsFrom = null,
    /// <summary>Optional regex pattern applied to the submitted value. If the value does not match, the submission is rejected.</summary>
    string? ValidationRegex = null,
    /// <summary>Human-readable message returned when ValidationRegex does not match.</summary>
    string? ValidationMessage = null,
    /// <summary>For file fields: accepted MIME types or extensions (e.g. ".pdf,.png,image/*"). Passed to the HTML input accept attribute.</summary>
    string? Accept = null
);
