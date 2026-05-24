namespace OrchestFlowAI.Engine.Validation;
/// <summary>
/// Represents an error encountered during workflow validation.
/// </summary>
/// <param name="NodeId">The ID of the node where the validation error occurred.</param>
/// <param name="Message">Details about the validation error.</param>
public sealed record ValidationError(string NodeId, string Message);
/// <summary>
/// Represents the result of a workflow validation process.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
/// Indicates whether the workflow is valid (i.e., no validation errors).
/// </summary>
public bool IsValid => !Errors.Any();
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();
    /// <summary>
/// Factory method to create a successful validation result (no errors).
/// </summary>
/// <returns>A ValidationResult indicating success.</returns>
public static ValidationResult Success() => new() { Errors = Array.Empty<ValidationError>() };
    /// <summary>
/// Factory method to create a validation result with errors.
/// </summary>
/// <param name="errors">A collection of validation errors.</param>
/// <returns>A ValidationResult indicating failure.</returns>
public static ValidationResult Failure(IEnumerable<ValidationError> errors) => new() { Errors = errors.ToList().AsReadOnly() };
}