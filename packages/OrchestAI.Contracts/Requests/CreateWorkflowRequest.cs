using System.Text.Json;
namespace OrchestAI.Contracts.Requests;
public sealed record CreateWorkflowRequest(string Name, string Description, JsonElement Definition);