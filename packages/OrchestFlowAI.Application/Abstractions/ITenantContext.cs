namespace OrchestFlowAI.Application.Abstractions;
public interface ITenantContext { Guid TenantId { get; } Guid UserId { get; } string UserRole { get; } }