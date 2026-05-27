namespace OrchestFlowAI.Contracts.Responses;
public sealed record DocumentResponse(Guid Id, string Filename, string MimeType, long SizeBytes, string Sha256, DateTime CreatedAt);
public sealed record ExecutionTimelineResponse(Guid ExecutionId, IReadOnlyList<NodeExecutionResponse> Nodes);
public sealed record NodePortResponse(string Key, string DisplayName, string Description, string Type, bool? Required, object? DefaultValue, IReadOnlyCollection<string>? AllowedValues, string? OptionsSource = null, IReadOnlyDictionary<string, string>? OptionDescriptions = null);
public sealed record NodeDescriptorResponse(string Type, string DisplayName, string Description, string Category, string Version, string? IconKey, IReadOnlyCollection<NodePortResponse> Inputs, IReadOnlyCollection<NodePortResponse> Outputs, IReadOnlyCollection<NodePortResponse> Configuration);
