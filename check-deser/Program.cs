using System.Text.Json;
using OrchestFlowAI.Engine.Models;

var json = File.ReadAllText(args[0]);
var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var def = JsonSerializer.Deserialize<WorkflowDefinition>(json, opts)!;

Console.WriteLine($"Nodes: {def.Nodes.Count}");
Console.WriteLine($"Edges: {def.Edges.Count}");
foreach (var n in def.Nodes)
    Console.WriteLine($"  Node id={n.Id} type={n.Type} config_keys={string.Join(",", n.Config.Keys)}");
foreach (var e in def.Edges)
    Console.WriteLine($"  Edge {e.Source} -> {e.Target} map={e.Map != null}");
