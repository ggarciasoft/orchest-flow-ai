namespace OrchestAI.Domain.Entities;
public sealed class Document
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Filename { get; private set; } = default!;
    public string MimeType { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public string StorageUri { get; private set; } = default!;
    public string Sha256 { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    private Document() { }
    public static Document Create(Guid tenantId, Guid ownerId, string filename, string mimeType, long sizeBytes, string storageUri, string sha256)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, OwnerId = ownerId, Filename = filename, MimeType = mimeType, SizeBytes = sizeBytes, StorageUri = storageUri, Sha256 = sha256, CreatedAt = DateTime.UtcNow };
}