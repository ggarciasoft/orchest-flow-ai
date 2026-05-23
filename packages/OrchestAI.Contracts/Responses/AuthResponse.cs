namespace OrchestAI.Contracts.Responses;
public sealed record UserDto(Guid Id, string Email, string DisplayName, string Role);
public sealed record AuthResponse(string Token, UserDto User);