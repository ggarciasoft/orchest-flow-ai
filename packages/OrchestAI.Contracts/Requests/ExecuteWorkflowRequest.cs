using System.Text.Json;
namespace OrchestAI.Contracts.Requests;
public sealed record ExecuteWorkflowRequest(Dictionary<string, JsonElement>? Input);