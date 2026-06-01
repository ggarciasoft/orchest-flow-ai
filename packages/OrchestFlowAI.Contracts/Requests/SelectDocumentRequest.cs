namespace OrchestFlowAI.Contracts.Requests;

/// <summary>Request body for selecting a document to resume a document-selection approval.</summary>
public sealed record SelectDocumentRequest(Guid DocumentId, string Filename, string MimeType, long SizeBytes, string Sha256);
