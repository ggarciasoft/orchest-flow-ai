namespace OrchestFlowAI.Domain.Entities;

/// <summary>
/// A snapshot of a form's fields at a point in time.
/// The active version is what the engine uses at execution time.
/// </summary>
public sealed class FormVersion
{
    public Guid Id { get; private set; }
    public Guid FormId { get; private set; }
    public int VersionNumber { get; private set; }
    public string FieldsJson { get; private set; } = "[]";
    public bool IsActive { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private FormVersion() { }

    public static FormVersion Create(Guid formId, int versionNumber, string fieldsJson, Guid createdBy) => new()
    {
        Id = Guid.NewGuid(),
        FormId = formId,
        VersionNumber = versionNumber,
        FieldsJson = fieldsJson,
        IsActive = false,
        CreatedBy = createdBy,
        CreatedAt = DateTime.UtcNow,
    };

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
