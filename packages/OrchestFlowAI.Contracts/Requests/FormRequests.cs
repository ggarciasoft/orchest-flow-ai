using System.Text.Json;

namespace OrchestFlowAI.Contracts.Requests;

public sealed record CreateFormRequest(
    string Name,
    string Slug,
    string? Description,
    JsonElement Fields);

public sealed record UpdateFormRequest(
    string Name,
    string Slug,
    string? Description,
    JsonElement Fields);

public sealed record SubmitFormRequest(
    Guid WorkflowExecutionId,
    string NodeExecutionId,
    JsonElement Values);
