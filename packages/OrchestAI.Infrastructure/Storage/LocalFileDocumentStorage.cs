using OrchestAI.Application.Abstractions;
using Microsoft.Extensions.Configuration;
namespace OrchestAI.Infrastructure.Storage;

public sealed class LocalFileDocumentStorage : IDocumentStorage
{
    private readonly string _root;
    public LocalFileDocumentStorage(IConfiguration config) => _root = config["Storage:Root"] ?? "./data/uploads";

    public async Task<string> UploadAsync(Guid tenantId, Guid documentId, Stream content, string mimeType, CancellationToken ct = default)
    {
        var ext = mimeType switch { "application/pdf" => ".pdf", "image/png" => ".png", "image/jpeg" => ".jpg", _ => ".bin" };
        var relativePath = Path.Combine("tenants", tenantId.ToString(), $"{documentId}{ext}");
        var fullPath = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);
        return relativePath;
    }

    public Task<Stream> DownloadAsync(string storageUri, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, storageUri);
        if (!File.Exists(fullPath)) throw new FileNotFoundException($"Document not found: {storageUri}");
        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }
}