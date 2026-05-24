using OrchestFlowAI.Domain.Entities;
using FluentAssertions;

namespace OrchestFlowAI.Tests.DomainTests;

public class DocumentTests
{
    [Fact]
    public void Create_ShouldInitializeProperly()
    {
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var filename = "test.pdf";
        var mimeType = "application/pdf";
        var sizeBytes = 1024L;
        var storageUri = "http://example.com/test.pdf";
        var sha256 = "dummyhash";

        var document = Document.Create(tenantId, ownerId, filename, mimeType, sizeBytes, storageUri, sha256);

        document.TenantId.Should().Be(tenantId);
        document.OwnerId.Should().Be(ownerId);
        document.Filename.Should().Be(filename);
        document.MimeType.Should().Be(mimeType);
        document.SizeBytes.Should().Be(sizeBytes);
        document.StorageUri.Should().Be(storageUri);
        document.Sha256.Should().Be(sha256);
        document.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }
}