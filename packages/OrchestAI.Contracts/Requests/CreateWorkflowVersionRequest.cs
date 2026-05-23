using System.Text.Json;
namespace OrchestAI.Contracts.Requests;
public sealed record CreateWorkflowVersionRequest(JsonElement Definition);
