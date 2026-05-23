using OrchestAI.Engine.Models;
using OrchestAI.Engine.Registry;
namespace OrchestAI.Engine.Validation;

/// <summary>
/// Validates workflows by ensuring that all nodes and edges conform to expected structures
/// and practices within the OrchestAI environment.
/// </summary>
public sealed class WorkflowValidator
{
    public ValidationResult Validate(WorkflowDefinition def, INodeRegistry registry)
    {
        var errors = new List<ValidationError>();
        var nodeIds = def.Nodes.Select(n => n.Id).ToHashSet();

        var startCount = def.Nodes.Count(n => n.Type == "system.start");
        if (startCount != 1) errors.Add(new ValidationError("*", "Workflow must have exactly one system.start node."));

        var endCount = def.Nodes.Count(n => n.Type == "system.end");
        if (endCount < 1) errors.Add(new ValidationError("*", "Workflow must have at least one system.end node."));

        foreach (var node in def.Nodes)
        {
            if (registry.GetDescriptor(node.Type) == null)
                errors.Add(new ValidationError(node.Id, $"Unknown node type: {node.Type}"));
        }

        foreach (var edge in def.Edges)
        {
            if (!nodeIds.Contains(edge.Source)) errors.Add(new ValidationError(edge.Source, $"Edge source '{edge.Source}' not found."));
            if (!nodeIds.Contains(edge.Target)) errors.Add(new ValidationError(edge.Target, $"Edge target '{edge.Target}' not found."));
        }

        return errors.Count > 0 ? ValidationResult.Failure(errors) : ValidationResult.Success();
    }
}