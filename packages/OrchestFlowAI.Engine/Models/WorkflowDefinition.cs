using System.Text.Json;
namespace OrchestFlowAI.Engine.Models;

public sealed class WorkflowDefinition
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Version { get; set; }
    public List<WorkflowNodeDefinition> Nodes { get; set; } = new();
    public List<WorkflowEdgeDefinition> Edges { get; set; } = new();
}

public sealed class WorkflowNodeDefinition
{
    public string Id { get; set; } = default!;
    public string Type { get; set; } = default!;
    public NodePosition Position { get; set; } = new();
    public Dictionary<string, JsonElement> Config { get; set; } = new();
}

public sealed class WorkflowEdgeDefinition
{
    public string Source { get; set; } = default!;
    public string Target { get; set; } = default!;
    public string? Condition { get; set; }
    public Dictionary<string, string>? Map { get; set; }
}

public sealed record NodePosition(double X = 0, double Y = 0);