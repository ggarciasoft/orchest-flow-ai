using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Contracts.Responses;
using OrchestFlowAI.Domain.Entities;
using System.Security.Cryptography;
namespace OrchestFlowAI.Api.Controllers;

[ApiController, Route("api/documents"), Authorize]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentRepository _documents;
    private readonly IDocumentStorage _storage;
    public DocumentsController(IDocumentRepository documents, IDocumentStorage storage) { _documents = documents; _storage = storage; }
    private Guid TenantId => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());
    private Guid UserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpPost("upload")]
    public async Task<ActionResult<DocumentResponse>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest("No file provided");
        var docId = Guid.NewGuid();
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        var sha256 = Convert.ToHexString(SHA256.HashData(bytes));
        ms.Position = 0;
        var uri = await _storage.UploadAsync(TenantId, docId, ms, file.ContentType, ct);
        var doc = Document.Create(TenantId, UserId, file.FileName, file.ContentType, file.Length, uri, sha256);
        await _documents.CreateAsync(doc, ct);
        return Ok(new DocumentResponse(doc.Id, doc.Filename, doc.MimeType, doc.SizeBytes, doc.Sha256, doc.CreatedAt));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentResponse>> Get(Guid id, CancellationToken ct)
    {
        var doc = await _documents.GetAsync(id, TenantId, ct);
        if (doc == null) return NotFound();
        return Ok(new DocumentResponse(doc.Id, doc.Filename, doc.MimeType, doc.SizeBytes, doc.Sha256, doc.CreatedAt));
    }

    [HttpGet("{id}/content")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var doc = await _documents.GetAsync(id, TenantId, ct);
        if (doc == null) return NotFound();
        var stream = await _storage.DownloadAsync(doc.StorageUri, ct);
        return File(stream, doc.MimeType, doc.Filename);
    }
}
