using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Interfaces;
using OrchestFlowAI.SDK.Models;
namespace OrchestFlowAI.Nodes.Documents;

/// <summary>
/// Pauses workflow execution and prompts a human to select a document from the document library.
/// The workflow resumes with the selected document's metadata as outputs.
/// </summary>
public sealed class SelectDocumentNode : IWorkflowNode
{
    /// <inheritdoc />
    public string Type => "document.select";

    /// <inheritdoc />
    public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext ctx, CancellationToken ct)
    {
        var payload = new Dictionary<string, object?>();
        payload["_approvalKind"] = "DocumentSelection";
        payload["_approvalTitle"] = ctx.GetConfig<string>("title") ?? "Select a Document";
        payload["_prompt"] = ctx.GetConfig<string>("prompt") ?? "Please select a document to continue.";
        // Include any node inputs as context visible to the user
        foreach (var kv in ctx.NodeInputs)
            payload[kv.Key] = kv.Value;
        return Task.FromResult(NodeExecutionResult.WaitingForApproval(payload));
    }
}

/// <summary>Descriptor for <see cref="SelectDocumentNode"/> — provides metadata for the designer palette.</summary>
public sealed class SelectDocumentNodeDescriptor : IWorkflowNodeDescriptor
{
    /// <inheritdoc />
    public string Type => "document.select";
    /// <inheritdoc />
    public string DisplayName => "Select Document";
    /// <inheritdoc />
    public string Description => "Pauses the workflow and prompts a human to select a document from the document library.";
    /// <inheritdoc />
    public string Category => "documents";
    /// <inheritdoc />
    public string Version => "1.0.0";
    /// <inheritdoc />
    public string? IconKey => "file-search";
    /// <inheritdoc />
    public IReadOnlyCollection<NodeInputDefinition> Inputs => Array.Empty<NodeInputDefinition>();
    /// <inheritdoc />
    public IReadOnlyCollection<NodeOutputDefinition> Outputs => new[]
    {
        new NodeOutputDefinition("documentId", "Document ID", "ID of the selected document", DataType.String),
        new NodeOutputDefinition("filename", "Filename", "Name of the selected file", DataType.String),
        new NodeOutputDefinition("mimeType", "MIME Type", "MIME type of the selected file", DataType.String),
        new NodeOutputDefinition("sizeBytes", "Size (bytes)", "File size in bytes", DataType.Number),
        new NodeOutputDefinition("sha256", "SHA-256", "SHA-256 hash of the file", DataType.String),
    };
    /// <inheritdoc />
    public IReadOnlyCollection<NodeConfigDefinition> Configuration => new[]
    {
        new NodeConfigDefinition("title", "Title", "Title shown to the user in the inbox.", DataType.String, Required: false, DefaultValue: "Select a Document"),
        new NodeConfigDefinition("prompt", "Prompt", "Instructions shown above the document list.", DataType.String, Required: false),
    };
}
