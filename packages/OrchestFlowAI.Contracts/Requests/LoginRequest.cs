namespace OrchestFlowAI.Contracts.Requests;
public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(string DisplayName, string Email, string Password);