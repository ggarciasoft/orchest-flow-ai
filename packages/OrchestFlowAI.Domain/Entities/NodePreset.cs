using System;

namespace OrchestFlowAI.Domain.Entities;

public sealed class NodePreset
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string NodeType { get; private set; }  // e.g. "integrations.http"
    public string ConfigJson { get; private set; }  // JSON of config key-value pairs
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private NodePreset() { }

    public static NodePreset Create(Guid tenantId, string name, string nodeType, string configJson)
    {
        return new NodePreset
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            NodeType = nodeType,
            ConfigJson = configJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string configJson)
    {
        Name = name;
        ConfigJson = configJson;
        UpdatedAt = DateTime.UtcNow;
    }
}