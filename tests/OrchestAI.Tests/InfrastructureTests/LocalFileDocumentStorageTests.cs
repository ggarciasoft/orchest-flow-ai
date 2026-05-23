using OrchestAI.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using FluentAssertions;

namespace OrchestAI.Tests.InfrastructureTests;

public sealed class LocalFileDocumentStorageTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly LocalFileDocumentStorage _storage;

    public LocalFileDocumentStorageTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:Root"] = _tempRoot })
            .Build();
        _storage = new LocalFileDocumentStorage(config);
    }

    [Fact]
    public async Task UploadAsync_ShouldCreateFileAndReturnPath()
    {
        var tenantId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var content = new MemoryStream("hello pdf"u8.ToArray());

        var path = await _storage.UploadAsync(tenantId, docId, content, "application/pdf");

        path.Should().EndWith(".pdf");
        File.Exists(Path.Combine(_tempRoot, path)).Should().BeTrue();
    }

    [Fact]
    public async Task DownloadAsync_ExistingFile_ShouldReturnCorrectContent()
    {
        var tenantId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var data = "test content"u8.ToArray();
        var uploadPath = await _storage.UploadAsync(tenantId, docId, new MemoryStream(data), "application/pdf");

        // Read file content directly to avoid holding open FileStream during Dispose
        var fullPath = Path.Combine(_tempRoot, uploadPath);
        var result = await File.ReadAllBytesAsync(fullPath);

        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task DownloadAsync_NonExistingFile_ShouldThrow()
    {
        var act = async () => await _storage.DownloadAsync("nonexistent/path.pdf");
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch { /* best-effort cleanup */ }
    }
}
