namespace OrchestAI.Domain.ValueObjects;
public sealed record DocumentRef(Guid DocumentId, string MimeType, long SizeBytes);