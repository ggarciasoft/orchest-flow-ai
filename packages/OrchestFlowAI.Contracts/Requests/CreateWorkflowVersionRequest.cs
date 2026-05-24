using System.Text.Json;
namespace OrchestFlowAI.Contracts.Requests;
public sealed record CreateWorkflowVersionRequest(JsonElement Definition);
