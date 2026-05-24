using System.Text.Json;
namespace OrchestFlowAI.Contracts.Requests;
public sealed record ExecuteWorkflowRequest(Dictionary<string, JsonElement>? Input);