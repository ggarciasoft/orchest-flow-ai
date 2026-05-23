using System.Text.Json;
using OrchestAI.Domain.Entities;
using OrchestAI.Engine.Conditions;
using OrchestAI.Engine.Models;
using OrchestAI.Engine.Registry;
using OrchestAI.Engine.Validation;
using OrchestAI.SDK.Context;
using OrchestAI.SDK.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OrchestAI.Engine;

public sealed class WorkflowExecutionEngine : IWorkflowEngine
{
    private readonly IServiceProvider _services;
    private readonly INodeRegistry _registry;
    private readonly WorkflowValidator _validator;
    private readonly ExpressionEvaluator _evaluator;
    private readonly ILogger<WorkflowExecutionEngine> _logger;
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public WorkflowExecutionEngine(IServiceProvider services, INodeRegistry registry, ILogger<WorkflowExecutionEngine> logger)
    {
        _services = services;
        _registry = registry;
        _validator = new WorkflowValidator();
        _evaluator = new ExpressionEvaluator();
        _logger = logger;
    }

    public Task<ValidationResult> ValidateAsync(WorkflowDefinition def, CancellationToken ct = default)
        => Task.FromResult(_validator.Validate(def, _registry));

    public async Task RunAsync(Guid executionId, CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var execRepo = scope.ServiceProvider.GetRequiredService<IEngineExecutionRepository>();

        var execution = await execRepo.GetExecutionAsync(executionId, ct)
            ?? throw new InvalidOperationException($"Execution {executionId} not found");
        var version = await execRepo.GetWorkflowVersionAsync(execution.WorkflowVersionId, ct)
            ?? throw new InvalidOperationException($"Version {execution.WorkflowVersionId} not found");

        var def = JsonSerializer.Deserialize<WorkflowDefinition>(version.DefinitionJson, _jsonOpts)
            ?? throw new InvalidOperationException("Failed to deserialize workflow definition");
        var inputs = JsonSerializer.Deserialize<Dictionary<string, object?>>(execution.InputJson ?? "{}", _jsonOpts) ?? new();

        execution.Start();
        await execRepo.UpdateExecutionAsync(execution, ct);

        var nodeOutputs = new Dictionary<string, IReadOnlyDictionary<string, object?>>();
        var nodeMap = def.Nodes.ToDictionary(n => n.Id);
        var current = def.Nodes.FirstOrDefault(n => n.Type == "system.start")
            ?? throw new InvalidOperationException("No system.start node");

        int step = 0;
        while (current != null)
        {
            step++;
            var node = _registry.GetNode(current.Type);
            if (node == null)
            {
                execution.Fail($"Node type '{current.Type}' not found");
                await execRepo.UpdateExecutionAsync(execution, ct);
                return;
            }

            var nodeInputs = ResolveInputs(current.Id, def.Edges, nodeOutputs, inputs);
            var config = current.Config.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);

            var nodeExec = NodeExecution.Create(executionId, current.Id, current.Type, step);
            nodeExec.Start(JsonSerializer.Serialize(nodeInputs));
            await execRepo.CreateNodeExecutionAsync(nodeExec, ct);

            var ctx = BuildContext(executionId, execution, nodeInputs, config, nodeOutputs, inputs, step, scope.ServiceProvider, ct);

            NodeExecutionResult result;
            try { result = await node.ExecuteAsync(ctx, ct); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Node {NodeType} threw exception", current.Type);
                result = SDK.Models.NodeExecutionResult.Failed(ex.Message);
            }

            switch (result.Status)
            {
                case SDK.Models.NodeExecutionStatus.WaitingForApproval:
                    nodeExec.WaitForApproval(JsonSerializer.Serialize(result.Outputs));
                    await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
                    var approval = ApprovalRequest.Create(execution.TenantId, executionId, nodeExec.Id, JsonSerializer.Serialize(result.Outputs), execution.TriggeredBy);
                    await execRepo.CreateApprovalAsync(approval, ct);
                    execution.Pause();
                    await execRepo.UpdateExecutionAsync(execution, ct);
                    _logger.LogInformation("Execution {ExecutionId} paused for approval", executionId);
                    return;

                case SDK.Models.NodeExecutionStatus.Failed:
                    nodeExec.Fail(result.ErrorMessage ?? "error");
                    await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
                    if (!result.Retryable) { execution.Fail(result.ErrorMessage ?? "error"); await execRepo.UpdateExecutionAsync(execution, ct); }
                    return;

                case SDK.Models.NodeExecutionStatus.Skipped:
                    nodeExec.Skip();
                    await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
                    break;

                default:
                    nodeExec.Succeed(JsonSerializer.Serialize(result.Outputs));
                    await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
                    nodeOutputs[current.Id] = result.Outputs;
                    break;
            }

            if (current.Type == "system.end")
            {
                execution.Complete(JsonSerializer.Serialize(nodeOutputs));
                await execRepo.UpdateExecutionAsync(execution, ct);
                _logger.LogInformation("Execution {ExecutionId} completed", executionId);
                return;
            }

            current = ResolveNextNode(current.Id, def.Edges, nodeMap, result.Outputs, nodeOutputs);
        }

        execution.Complete(null);
        await execRepo.UpdateExecutionAsync(execution, ct);
    }

    public async Task ResumeAsync(Guid executionId, ResumeSignal signal, CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var execRepo = scope.ServiceProvider.GetRequiredService<IEngineExecutionRepository>();

        var execution = await execRepo.GetExecutionAsync(executionId, ct)
            ?? throw new InvalidOperationException($"Execution {executionId} not found");
        var nodeExec = await execRepo.GetNodeExecutionAsync(signal.NodeExecutionId, ct)
            ?? throw new InvalidOperationException($"NodeExecution {signal.NodeExecutionId} not found");

        nodeExec.Succeed(JsonSerializer.Serialize(signal.ResumeOutputs));
        await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
        execution.Resume();
        await execRepo.UpdateExecutionAsync(execution, ct);

        var version = await execRepo.GetWorkflowVersionAsync(execution.WorkflowVersionId, ct)!;
        var def = JsonSerializer.Deserialize<WorkflowDefinition>(version!.DefinitionJson, _jsonOpts)!;
        var nodeMap = def.Nodes.ToDictionary(n => n.Id);

        var allNodeExecs = await execRepo.GetNodeExecutionsAsync(executionId, ct);
        var nodeOutputs = new Dictionary<string, IReadOnlyDictionary<string, object?>>();
        foreach (var ne in allNodeExecs.Where(n => n.OutputJson != null && n.Status == Domain.Enums.NodeExecutionStatus.Succeeded))
        {
            var outputs = JsonSerializer.Deserialize<Dictionary<string, object?>>(ne.OutputJson!, _jsonOpts);
            if (outputs != null) nodeOutputs[ne.NodeId] = outputs;
        }
        nodeOutputs[nodeExec.NodeId] = signal.ResumeOutputs;

        var inputs = JsonSerializer.Deserialize<Dictionary<string, object?>>(execution.InputJson ?? "{}", _jsonOpts) ?? new();
        var resumeOutputsDict = signal.ResumeOutputs.ToDictionary(kv => kv.Key, kv => kv.Value);
        var current = ResolveNextNode(nodeExec.NodeId, def.Edges, nodeMap, resumeOutputsDict, nodeOutputs);
        int step = allNodeExecs.Count;

        while (current != null)
        {
            step++;
            var node = _registry.GetNode(current.Type);
            if (node == null) { execution.Fail($"Node '{current.Type}' not found"); await execRepo.UpdateExecutionAsync(execution, ct); return; }

            var nodeInputs = ResolveInputs(current.Id, def.Edges, nodeOutputs, inputs);
            var config = current.Config.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);

            var ne2 = NodeExecution.Create(executionId, current.Id, current.Type, step);
            ne2.Start(JsonSerializer.Serialize(nodeInputs));
            await execRepo.CreateNodeExecutionAsync(ne2, ct);

            var ctx = BuildContext(executionId, execution, nodeInputs, config, nodeOutputs, inputs, step, scope.ServiceProvider, ct);

            NodeExecutionResult result;
            try { result = await node.ExecuteAsync(ctx, ct); }
            catch (Exception ex) { result = SDK.Models.NodeExecutionResult.Failed(ex.Message); }

            switch (result.Status)
            {
                case SDK.Models.NodeExecutionStatus.Succeeded:
                    ne2.Succeed(JsonSerializer.Serialize(result.Outputs));
                    nodeOutputs[current.Id] = result.Outputs;
                    break;
                case SDK.Models.NodeExecutionStatus.Skipped:
                    ne2.Skip();
                    break;
                default:
                    ne2.Fail(result.ErrorMessage ?? "error");
                    execution.Fail(result.ErrorMessage ?? "error");
                    await execRepo.UpdateNodeExecutionAsync(ne2, ct);
                    await execRepo.UpdateExecutionAsync(execution, ct);
                    return;
            }

            await execRepo.UpdateNodeExecutionAsync(ne2, ct);

            if (current.Type == "system.end")
            {
                execution.Complete(JsonSerializer.Serialize(nodeOutputs));
                await execRepo.UpdateExecutionAsync(execution, ct);
                return;
            }

            current = ResolveNextNode(current.Id, def.Edges, nodeMap, result.Outputs, nodeOutputs);
        }

        execution.Complete(null);
        await execRepo.UpdateExecutionAsync(execution, ct);
    }

    private WorkflowExecutionContext BuildContext(Guid executionId, WorkflowExecution execution,
        Dictionary<string, object?> nodeInputs, Dictionary<string, object?> config,
        Dictionary<string, IReadOnlyDictionary<string, object?>> nodeOutputs,
        Dictionary<string, object?> workflowInputs, int step, IServiceProvider services, CancellationToken ct) => new()
    {
        ExecutionId = executionId,
        WorkflowId = execution.WorkflowId,
        WorkflowVersionId = execution.WorkflowVersionId,
        TenantId = execution.TenantId,
        TriggeredByUserId = execution.TriggeredBy,
        CorrelationId = execution.CorrelationId,
        WorkflowInputs = workflowInputs,
        NodeOutputs = nodeOutputs,
        NodeInputs = nodeInputs,
        NodeConfig = config,
        Step = step,
        Services = services,
        CancellationToken = ct
    };

    private WorkflowNodeDefinition? ResolveNextNode(string sourceId, List<WorkflowEdgeDefinition> edges,
        Dictionary<string, WorkflowNodeDefinition> nodeMap, Dictionary<string, object?> currentOutputs,
        Dictionary<string, IReadOnlyDictionary<string, object?>> allOutputs)
    {
        var outEdges = edges.Where(e => e.Source == sourceId).ToList();
        foreach (var edge in outEdges)
        {
            if (string.IsNullOrEmpty(edge.Condition)) return nodeMap.GetValueOrDefault(edge.Target);
            var scope = new Dictionary<string, object?>(currentOutputs);
            foreach (var kv2 in allOutputs) foreach (var pair in kv2.Value) scope.TryAdd(pair.Key, pair.Value);
            if (_evaluator.Evaluate(edge.Condition, scope)) return nodeMap.GetValueOrDefault(edge.Target);
        }
        return null;
    }

    private static Dictionary<string, object?> ResolveInputs(string nodeId, List<WorkflowEdgeDefinition> edges,
        Dictionary<string, IReadOnlyDictionary<string, object?>> nodeOutputs, Dictionary<string, object?> workflowInputs)
    {
        var inputs = new Dictionary<string, object?>(workflowInputs);
        foreach (var edge in edges.Where(e => e.Target == nodeId))
        {
            if (!nodeOutputs.TryGetValue(edge.Source, out var sourceOutputs)) continue;
            if (edge.Map != null)
                foreach (var (targetKey, sourceKey) in edge.Map)
                    if (sourceOutputs.TryGetValue(sourceKey, out var mappedVal)) inputs[targetKey] = mappedVal;
            else
                foreach (var (k, v) in sourceOutputs) inputs[k] = v;
        }
        return inputs;
    }
}
