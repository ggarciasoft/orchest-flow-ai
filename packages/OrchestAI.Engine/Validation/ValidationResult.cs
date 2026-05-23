namespace OrchestAI.Engine.Validation;
public sealed record ValidationError(string NodeId, string Message);
public sealed class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();
    public static ValidationResult Success() => new() { Errors = Array.Empty<ValidationError>() };
    public static ValidationResult Failure(IEnumerable<ValidationError> errors) => new() { Errors = errors.ToList().AsReadOnly() };
}