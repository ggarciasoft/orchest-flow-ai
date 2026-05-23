using OrchestAI.Application.Abstractions;
using OrchestAI.SDK.Context;
using OrchestAI.SDK.Interfaces;
using OrchestAI.SDK.Models;
using UglyToad.PdfPig;
using Microsoft.Extensions.DependencyInjection;
namespace OrchestAI.Nodes.Documents;

public sealed class ExtractPdfTextNode : IWorkflowNode
{
    public string Type => "document.extract-pdf-text";
    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var documentId = ctx.GetInput<Guid>("documentId");
        if (documentId == Guid.Empty) return NodeExecutionResult.Failed("Input 'documentId' is required");
        var storage = ctx.Services.GetRequiredService<IDocumentStorage>();
        var docRepo = ctx.Services.GetRequiredService<IDocumentRepository>();
        var doc = await docRepo.GetAsync(documentId, ctx.TenantId, ct);
        if (doc == null) return NodeExecutionResult.Failed($"Document {documentId} not found");
        using var stream = await storage.DownloadAsync(doc.StorageUri, ct);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        ms.Position = 0;
        using var pdf = PdfDocument.Open(ms.ToArray());
        var text = string.Join("\n", pdf.GetPages().Select(p => p.Text));
        return NodeExecutionResult.Succeeded(new Dictionary<string, object?>
        {
            ["text"] = text,
            ["pageCount"] = pdf.NumberOfPages
        });
    }
}

public sealed class ExtractPdfTextNodeDescriptor : IWorkflowNodeDescriptor
{
    public string Type => "document.extract-pdf-text";
    public string DisplayName => "Extract PDF Text";
    public string Description => "Extracts plain text from a PDF document.";
    public string Category => "documents";
    public string Version => "1.0.0";
    public string? IconKey => "file-pdf";
    public IReadOnlyCollection<NodeInputDefinition> Inputs => new[] { new NodeInputDefinition("documentId", "Document ID", "ID of the uploaded PDF document.", DataType.DocumentRef, Required: true) };
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("text", "Text", "Extracted plain text.", DataType.String),
        new NodeOutputDefinition("pageCount", "Page Count", "Number of pages.", DataType.Number)
    };
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("ocrFallback", "OCR Fallback", "Use OCR if text extraction fails.", DataType.Boolean, Required: false, DefaultValue: false)
    };
}
