namespace OrchestAI.Application.Abstractions;
public interface IDocumentStorage
{
    Task<string> UploadAsync(Guid tenantId, Guid documentId, Stream content, string mimeType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string storageUri, CancellationToken ct = default);
}