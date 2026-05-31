using System.Text.Json;
using OrchestFlowAI.Contracts.Notifications;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.ValueObjects;
using OrchestFlowAI.Engine.Conditions;
using OrchestFlowAI.Engine.Models;
using OrchestFlowAI.Engine.Registry;
using OrchestFlowAI.Engine.Validation;
using OrchestFlowAI.SDK.Context;
using OrchestFlowAI.SDK.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OrchestFlowAI.Engine;

/// <summary>
/// The core engine for executing workflows in the OrchestFlowAI platform.
/// This engine validates, executes, and manages workflow states and transitions during their lifecycle.
/// </summary>
public sealed class WorkflowExecutionEngine : IWorkflowEngine
{
    private readonly IServiceProvider _services;
    private readonly INodeRegistry _registry;
    private readonly IExecutionNotifier _notifier;
    private readonly WorkflowValidator _validator;
    private readonly ExpressionEvaluator _evaluator;
    private readonly ILogger<WorkflowExecutionEngine> _logger;
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public WorkflowExecutionEngine(IServiceProvider services, INodeRegistry registry, IExecutionNotifier notifier, ILogger<WorkflowExecutionEngine> logger)
    {
        _services = services;
        _registry = registry;
        _notifier = notifier;
        _validator = new WorkflowValidator();
        _evaluator = new ExpressionEvaluator();
        _logger = logger;
    }

    /// <summary>
/// Validates the workflow definition against the registered node types.
/// </summary>
/// <param name="def">The workflow definition to validate.</param>
/// <param name="ct">Cancellation token for the operation.</param>
/// <returns>A ValidationResult indicating whether the workflow is valid.</returns>
public Task<ValidationResult> ValidateAsync(WorkflowDefinition def, CancellationToken ct = default)
        => Task.FromResult(_validator.Validate(def, _registry));

    /// <summary>
/// Executes a workflow based on its specific execution ID.
/// </summary>
/// <param name="executionId">The ID of the workflow execution to run.</param>
/// <param name="ct">Cancellation token for the operation.</param>
/// <returns>A Task representing the asynchronous operation.</returns>
public async Task RunAsync(Guid executionId, CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var execRepo = scope.ServiceProvider.GetRequiredService<IEngineExecutionRepository>();

        var execution = await execRepo.GetExecutionAsync(executionId, ct)
            ?? throw new InvalidOperationException($"Execution {executionId} not found");
        var version = await execRepo.GetWorkflowVersionAsync(execution.WorkflowVersionId, ct)
            ?? throw new InvalidOperationException($"Version {execution.WorkflowVersionId} not found");

        // Load workflow to access the retry policy
        var workflow = await execRepo.GetWorkflowAsync(execution.WorkflowId, ct);
        var retryPolicy = workflow?.RetryPolicy ?? RetryPolicy.None;

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

            // Resolve {{secret:name}} placeholders before passing config to the node
            var secretService = scope.ServiceProvider.GetService<ISecretResolver>();
            if (secretService != null)
                config = await secretService.ResolveConfigAsync(config, execution.TenantId, ct);

            var nodeExec = NodeExecution.Create(executionId, current.Id, current.Type, step);
            nodeExec.Start(JsonSerializer.Serialize(nodeInputs));
            await execRepo.CreateNodeExecutionAsync(nodeExec, ct);

            var ctx = BuildContext(executionId, execution, nodeInputs, config, nodeOutputs, inputs, step, scope.ServiceProvider, ct, nodeExec.Id);

            await _notifier.NotifyNodeStarted(executionId, nodeExec.Id, current.Type, ct);

            NodeExecutionResult result;
            try { result = await node.ExecuteAsync(ctx, ct); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Node {NodeType} threw exception", current.Type);
                result = SDK.Models.NodeExecutionResult.Failed(ex.Message);
            }

            // Retry loop: attempt additional tries if the policy allows and the result is failed
            while (result.Status == SDK.Models.NodeExecutionStatus.Failed
                   && nodeExec.AttemptNumber < retryPolicy.MaxAttempts)
            {
                var delay = retryPolicy.GetDelay(nodeExec.AttemptNumber);
                _logger.LogWarning(
                    "Node {NodeType} failed (attempt {Attempt}/{Max}). Retrying in {DelayMs}ms. ExecutionId={ExecutionId}",
                    current.Type, nodeExec.AttemptNumber, retryPolicy.MaxAttempts, delay.TotalMilliseconds, executionId);
                nodeExec.IncrementAttempt();
                await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
                await Task.Delay(delay, ct);
                nodeExec.Start(nodeExec.InputJson);
                await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
                ctx = BuildContext(executionId, execution, nodeInputs, config, nodeOutputs, inputs, step, scope.ServiceProvider, ct, nodeExec.Id);
                try { result = await node.ExecuteAsync(ctx, ct); }
                catch (Exception retryEx)
                {
                    _logger.LogError(retryEx, "Node {NodeType} threw exception on retry attempt {Attempt}", current.Type, nodeExec.AttemptNumber);
                    result = SDK.Models.NodeExecutionResult.Failed(retryEx.Message);
                }
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
                    await _notifier.NotifyNodeFailed(executionId, nodeExec.Id, current.Type, result.ErrorMessage ?? "error", ct);
                    // Mark execution as failed — retries have already been exhausted by the while loop above
                    execution.Fail(result.ErrorMessage ?? "error");
                    await execRepo.UpdateExecutionAsync(execution, ct);
                    await _notifier.NotifyExecutionCompleted(executionId, "Failed", ct);
                    return;

                case SDK.Models.NodeExecutionStatus.Skipped:
                    nodeExec.Skip();
                    await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
                    break;

                default:
                    nodeExec.Succeed(JsonSerializer.Serialize(result.Outputs));
                    await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
                    await _notifier.NotifyNodeCompleted(executionId, nodeExec.Id, current.Type, ct);
                    nodeOutputs[current.Id] = result.Outputs;
                    break;
            }

            if (current.Type == "system.end")
            {
                execution.Complete(JsonSerializer.Serialize(nodeOutputs));
                await execRepo.UpdateExecutionAsync(execution, ct);
                await _notifier.NotifyExecutionCompleted(executionId, "Completed", ct);
                _logger.LogInformation("Execution {ExecutionId} completed", executionId);
                return;
            }

            // ForEach loop mode: if this node emitted _foreach_items, run the body subgraph once per item
            if (current.Type == "logic.foreach"
                && nodeOutputs.TryGetValue(current.Id, out var feOutputs)
                && feOutputs.TryGetValue("_foreach_items", out var feItemsRaw)
                && feItemsRaw != null)
            {
                var itemsJson = feItemsRaw.ToString()!;
                var feItems = JsonSerializer.Deserialize<List<JsonElement>>(itemsJson, _jsonOpts) ?? new();
                var loopResults = new List<Dictionary<string, object?>>(); 
                // inheritOutputs: when true, each body node receives all outputs accumulated so far in the iteration.
                // Keeps things simple without needing explicit wiring for every pair. Off by default.
                var inheritOutputs = current.Config.TryGetValue("inheritOutputs", out var inhEl)
                    && inhEl is JsonElement inh
                    && (inh.ValueKind == JsonValueKind.True || (inh.ValueKind == JsonValueKind.String && inh.GetString() == "true"));

                // Find body start (first node after ForEach)
                var bodyStart = ResolveNextNode(current.Id, def.Edges, nodeMap, result.Outputs, nodeOutputs);

                foreach (var (item, idx) in feItems.Select((x, i) => (x, i)))
                {
                    // Clone nodeOutputs, inject current item as ForEach outputs
                    var iterOutputs = new Dictionary<string, IReadOnlyDictionary<string, object?>>(nodeOutputs);
                    iterOutputs[current.Id] = new Dictionary<string, object?>
                    {
                        ["item"] = item.GetRawText(),
                        ["index"] = (object?)idx,
                        ["total"] = feItems.Count
                    };

                    var iterNode = bodyStart;
                    Dictionary<string, object?>? lastIterOutputs = null;
                    while (iterNode != null && iterNode.Type != "logic.foreach.end")
                    {
                        var iterNodeImpl = _registry.GetNode(iterNode.Type);
                        if (iterNodeImpl == null) break;

                        // Build base inputs: use accumulated iteration outputs when inheritOutputs is on,
                        // otherwise use normal workflow inputs only. Direct edge wiring always takes priority.
                        Dictionary<string, object?> baseInputs;
                        if (inheritOutputs)
                        {
                            baseInputs = new Dictionary<string, object?>(inputs);
                            foreach (var nodeOut in iterOutputs.Values)
                                foreach (var kv in nodeOut)
                                    baseInputs.TryAdd(kv.Key, kv.Value);
                        }
                        else
                        {
                            baseInputs = inputs;
                        }

                        var iterInputs = ResolveInputs(iterNode.Id, def.Edges, iterOutputs, baseInputs);
                        _logger.LogDebug("Loop body ResolveInputs for {NodeId}: edgeCount={EdgeCount} iterOutputsKeys=[{Keys}] resultKeys=[{ResultKeys}]",
                            iterNode.Id, def.Edges.Count(e => e.Target == iterNode.Id),
                            string.Join(",", iterOutputs.Keys.Select(k => k.Split('-')[0])),
                            string.Join(",", iterInputs.Keys));
                        var iterConfig = iterNode.Config.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);

                        // Record each loop body node execution in the DB so it appears in the timeline
                        var iterNodeExec = NodeExecution.Create(executionId, iterNode.Id, iterNode.Type, ++step);
                        iterNodeExec.Start(JsonSerializer.Serialize(iterInputs));
                        await execRepo.CreateNodeExecutionAsync(iterNodeExec, ct);
                        var iterCtx = BuildContext(executionId, execution, iterInputs, iterConfig, iterOutputs, inputs, step, scope.ServiceProvider, ct, iterNodeExec.Id);
                        await _notifier.NotifyNodeStarted(executionId, iterNodeExec.Id, iterNode.Type, ct);

                        NodeExecutionResult iterResult;
                        try { iterResult = await iterNodeImpl.ExecuteAsync(iterCtx, ct); }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "ForEach body node {NodeType} threw on item {Idx}", iterNode.Type, idx);
                            iterResult = SDK.Models.NodeExecutionResult.Failed(ex.Message);
                        }

                        // Retry loop for body nodes — same policy as the main path
                        while (iterResult.Status == SDK.Models.NodeExecutionStatus.Failed
                               && iterNodeExec.AttemptNumber < retryPolicy.MaxAttempts)
                        {
                            var iterDelay = retryPolicy.GetDelay(iterNodeExec.AttemptNumber);
                            _logger.LogWarning(
                                "ForEach body node {NodeType} failed (attempt {Attempt}/{Max}). Retrying in {DelayMs}ms. Item={Idx}",
                                iterNode.Type, iterNodeExec.AttemptNumber, retryPolicy.MaxAttempts, iterDelay.TotalMilliseconds, idx);
                            iterNodeExec.IncrementAttempt();
                            await execRepo.UpdateNodeExecutionAsync(iterNodeExec, ct);
                            await Task.Delay(iterDelay, ct);
                            iterNodeExec.Start(iterNodeExec.InputJson);
                            await execRepo.UpdateNodeExecutionAsync(iterNodeExec, ct);
                            iterCtx = BuildContext(executionId, execution, iterInputs, iterConfig, iterOutputs, inputs, step, scope.ServiceProvider, ct, iterNodeExec.Id);
                            try { iterResult = await iterNodeImpl.ExecuteAsync(iterCtx, ct); }
                            catch (Exception retryEx)
                            {
                                _logger.LogWarning(retryEx, "ForEach body node {NodeType} threw on retry, item {Idx}", iterNode.Type, idx);
                                iterResult = SDK.Models.NodeExecutionResult.Failed(retryEx.Message);
                            }
                        }

                        if (iterResult.Status == SDK.Models.NodeExecutionStatus.Succeeded)
                        {
                            iterNodeExec.Succeed(JsonSerializer.Serialize(iterResult.Outputs));
                            await execRepo.UpdateNodeExecutionAsync(iterNodeExec, ct);
                            await _notifier.NotifyNodeCompleted(executionId, iterNodeExec.Id, iterNode.Type, ct);
                            iterOutputs[iterNode.Id] = iterResult.Outputs;
                            lastIterOutputs = new Dictionary<string, object?>(iterResult.Outputs);
                        }
                        else
                        {
                            iterNodeExec.Fail(iterResult.ErrorMessage ?? "error");
                            await execRepo.UpdateNodeExecutionAsync(iterNodeExec, ct);
                            await _notifier.NotifyNodeFailed(executionId, iterNodeExec.Id, iterNode.Type, iterResult.ErrorMessage ?? "error", ct);
                            // Propagate body failure to the workflow execution — same behaviour as the main path
                            execution.Fail(iterResult.ErrorMessage ?? "ForEach body node failed");
                            await execRepo.UpdateExecutionAsync(execution, ct);
                            await _notifier.NotifyExecutionCompleted(executionId, "Failed", ct);
                            return;
                        }

                        iterNode = ResolveNextNode(iterNode.Id, def.Edges, nodeMap,
                            iterResult.Outputs.ToDictionary(kv => kv.Key, kv => kv.Value), iterOutputs);
                    }

                    // Collect outputs from foreach.end node (passthrough) or last body node
                    if (iterNode != null && iterNode.Type == "logic.foreach.end")
                    {
                        var endInputs = ResolveInputs(iterNode.Id, def.Edges, iterOutputs, inputs);
                        var endResult = new Dictionary<string, object?>(endInputs);
                        loopResults.Add(endResult);
                    }
                    else if (lastIterOutputs != null)
                    {
                        loopResults.Add(lastIterOutputs);
                    }
                }

                // Replace ForEach outputs with collected results
                nodeOutputs[current.Id] = new Dictionary<string, object?>
                {
                    ["results"] = JsonSerializer.Serialize(loopResults),
                    ["count"] = feItems.Count
                };

                // Advance past the foreach.end node
                var scanNode = ResolveNextNode(current.Id, def.Edges, nodeMap, result.Outputs, nodeOutputs);
                while (scanNode != null && scanNode.Type != "logic.foreach.end")
                    scanNode = ResolveNextNode(scanNode.Id, def.Edges, nodeMap,
                        nodeOutputs.TryGetValue(scanNode.Id, out var sOut)
                            ? sOut.ToDictionary(kv => kv.Key, kv => kv.Value)
                            : new Dictionary<string, object?>(), nodeOutputs);

                current = scanNode != null
                    ? ResolveNextNode(scanNode.Id, def.Edges, nodeMap,
                        nodeOutputs[current.Id].ToDictionary(kv => kv.Key, kv => kv.Value), nodeOutputs)
                    : null;
                continue; // skip normal ResolveNextNode
            }

            // Fan-out: get all matching next nodes
            var nextNodes = ResolveNextNodes(current.Id, def.Edges, nodeMap, result.Outputs, nodeOutputs);

            if (nextNodes.Count == 0)
            {
                current = null;
            }
            else if (nextNodes.Count == 1)
            {
                current = nextNodes[0];
            }
            else
            {
                // Fan-out: execute all branch nodes sequentially
                foreach (var branchNode in nextNodes.Where(n => n != null))
                {
                    if (branchNode!.Type == "system.end")
                    {
                        // Branch leads to end — execute it and complete
                        var (endResult, endStep) = await ExecuteNodeAsync(branchNode, executionId, execution, def, nodeOutputs, nodeMap, inputs, step, execRepo, retryPolicy, scope.ServiceProvider, ct);
                        step = endStep;
                        if (endResult != null && endResult.Status == SDK.Models.NodeExecutionStatus.Succeeded)
                            nodeOutputs[branchNode.Id] = endResult.Outputs;
                        execution.Complete(JsonSerializer.Serialize(nodeOutputs));
                        await execRepo.UpdateExecutionAsync(execution, ct);
                        await _notifier.NotifyExecutionCompleted(executionId, "Completed", ct);
                        return;
                    }

                    var (fanResult, fanStep) = await ExecuteNodeAsync(branchNode, executionId, execution, def, nodeOutputs, nodeMap, inputs, step, execRepo, retryPolicy, scope.ServiceProvider, ct);
                    step = fanStep;
                    if (fanResult != null && fanResult.Status == SDK.Models.NodeExecutionStatus.Succeeded)
                        nodeOutputs[branchNode.Id] = fanResult.Outputs;
                    else if (fanResult?.Status != SDK.Models.NodeExecutionStatus.Skipped)
                        _logger.LogWarning("Fan-out branch node {NodeId} failed, skipping branch", branchNode.Id);
                }

                // Find convergence: first node that all branches connect to
                current = FindConvergenceNode(nextNodes!, def.Edges, nodeMap);
            }
        }

        execution.Complete(null);
        await execRepo.UpdateExecutionAsync(execution, ct);
        await _notifier.NotifyExecutionCompleted(executionId, "Completed", ct);
    }

    /// <summary>
/// Resumes a suspended workflow execution using the provided signal.
/// </summary>
/// <param name="executionId">The ID of the workflow execution to resume.</param>
/// <param name="signal">The resume signal containing the outputs to continue with.</param>
/// <param name="ct">Cancellation token for the operation.</param>
/// <returns>A Task representing the asynchronous operation.</returns>
public async Task ResumeAsync(Guid executionId, ResumeSignal signal, CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var execRepo = scope.ServiceProvider.GetRequiredService<IEngineExecutionRepository>();

        var execution = await execRepo.GetExecutionAsync(executionId, ct)
            ?? throw new InvalidOperationException($"Execution {executionId} not found");
        var nodeExec = await execRepo.GetNodeExecutionAsync(signal.NodeExecutionId, ct)
            ?? throw new InvalidOperationException($"NodeExecution {signal.NodeExecutionId} not found");

        // Re-run the paused node with resume outputs as inputs so node-level validation fires.
        var pausedNode = _registry.GetNode(nodeExec.NodeType);
        if (pausedNode != null)
        {
            var resumeNodeInputs = signal.ResumeOutputs.ToDictionary(kv => kv.Key, kv => kv.Value);
            var resumeNodeConfig = new Dictionary<string, object?>();

            // Load the workflow definition to get the node's config
            var versionForValidation = await execRepo.GetWorkflowVersionAsync(execution.WorkflowVersionId, ct);
            if (versionForValidation != null)
            {
                var defForValidation = JsonSerializer.Deserialize<WorkflowDefinition>(versionForValidation.DefinitionJson, _jsonOpts);
                var pausedNodeDef = defForValidation?.Nodes.FirstOrDefault(n => n.Id == nodeExec.NodeId);
                if (pausedNodeDef != null)
                    resumeNodeConfig = pausedNodeDef.Config.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
            }

            var validationCtx = BuildContext(executionId, execution, resumeNodeInputs, resumeNodeConfig,
                new Dictionary<string, IReadOnlyDictionary<string, object?>>(),
                new Dictionary<string, object?>(), nodeExec.Step, scope.ServiceProvider, ct, nodeExec.Id);

            NodeExecutionResult validationResult;
            try { validationResult = await pausedNode.ExecuteAsync(validationCtx, ct); }
            catch (Exception ex) { validationResult = SDK.Models.NodeExecutionResult.Failed(ex.Message); }

            if (validationResult.Status == SDK.Models.NodeExecutionStatus.Failed)
            {
                nodeExec.Fail(validationResult.ErrorMessage ?? "Validation failed");
                await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
                execution.Fail(validationResult.ErrorMessage ?? "Validation failed");
                await execRepo.UpdateExecutionAsync(execution, ct);
                await _notifier.NotifyExecutionCompleted(executionId, "Failed", ct);
                return;
            }

            // Use the node's own outputs (may differ from raw signal outputs, e.g. coerced types)
            var finalOutputs = validationResult.Status == SDK.Models.NodeExecutionStatus.Succeeded
                ? validationResult.Outputs
                : signal.ResumeOutputs;

            nodeExec.Succeed(JsonSerializer.Serialize(finalOutputs));
            // Override signal.ResumeOutputs so the rest of the method uses the node's processed outputs
            signal = signal with { ResumeOutputs = finalOutputs };
        }
        else
        {
            nodeExec.Succeed(JsonSerializer.Serialize(signal.ResumeOutputs));
        }

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

            // Resolve {{secret:name}} placeholders before passing config to the node
            var secretService2 = scope.ServiceProvider.GetService<ISecretResolver>();
            if (secretService2 != null)
                config = await secretService2.ResolveConfigAsync(config, execution.TenantId, ct);

            var ne2 = NodeExecution.Create(executionId, current.Id, current.Type, step);
            ne2.Start(JsonSerializer.Serialize(nodeInputs));
            await execRepo.CreateNodeExecutionAsync(ne2, ct);

            var ctx = BuildContext(executionId, execution, nodeInputs, config, nodeOutputs, inputs, step, scope.ServiceProvider, ct, ne2.Id);

            NodeExecutionResult result;
            try { result = await node.ExecuteAsync(ctx, ct); }
            catch (Exception ex) { result = SDK.Models.NodeExecutionResult.Failed(ex.Message); }

            switch (result.Status)
            {
                case SDK.Models.NodeExecutionStatus.WaitingForApproval:
                    // Next node in the chain is also a pause node (e.g. a second form step).
                    // Create an approval request and suspend exactly as RunAsync does.
                    ne2.WaitForApproval(JsonSerializer.Serialize(result.Outputs));
                    await execRepo.UpdateNodeExecutionAsync(ne2, ct);
                    var nextApproval = ApprovalRequest.Create(
                        execution.TenantId, executionId, ne2.Id,
                        JsonSerializer.Serialize(result.Outputs), execution.TriggeredBy);
                    await execRepo.CreateApprovalAsync(nextApproval, ct);
                    execution.Pause();
                    await execRepo.UpdateExecutionAsync(execution, ct);
                    _logger.LogInformation("Execution {ExecutionId} paused again for approval on node {NodeId}", executionId, current.Id);
                    return;

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

    /// <summary>
/// Cancels a workflow execution that is Queued, Running, or Paused.
/// In-flight node executions are marked as Cancelled; the execution record is also set to Cancelled.
/// </summary>
public async Task CancelAsync(Guid executionId, CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var execRepo = scope.ServiceProvider.GetRequiredService<IEngineExecutionRepository>();

        var execution = await execRepo.GetExecutionAsync(executionId, ct)
            ?? throw new InvalidOperationException($"Execution {executionId} not found");

        if (execution.Status is Domain.Enums.ExecutionStatus.Completed
            or Domain.Enums.ExecutionStatus.Failed
            or Domain.Enums.ExecutionStatus.Cancelled)
            throw new InvalidOperationException($"Execution is already in terminal state: {execution.Status}");

        // Mark any in-progress node executions as Cancelled
        var nodeExecs = await execRepo.GetNodeExecutionsAsync(executionId, ct);
        foreach (var ne in nodeExecs.Where(n =>
            n.Status is Domain.Enums.NodeExecutionStatus.Running
            or Domain.Enums.NodeExecutionStatus.WaitingForApproval))
        {
            ne.Cancel();
            await execRepo.UpdateNodeExecutionAsync(ne, ct);
        }

        // Cancel any pending approval request so it no longer shows up in the inbox
        var pendingApproval = await execRepo.GetPendingApprovalByExecutionIdAsync(executionId, ct);
        if (pendingApproval != null)
        {
            pendingApproval.Cancel();
            await execRepo.UpdateApprovalAsync(pendingApproval, ct);
        }

        execution.Cancel();
        await execRepo.UpdateExecutionAsync(execution, ct);
        await _notifier.NotifyExecutionCompleted(executionId, "Cancelled", ct);
        _logger.LogInformation("Execution {ExecutionId} cancelled by user", executionId);
    }

    private WorkflowExecutionContext BuildContext(Guid executionId, WorkflowExecution execution,
        Dictionary<string, object?> nodeInputs, Dictionary<string, object?> config,
        Dictionary<string, IReadOnlyDictionary<string, object?>> nodeOutputs,
        Dictionary<string, object?> workflowInputs, int step, IServiceProvider services, CancellationToken ct,
        Guid nodeExecutionId = default) => new()
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
        NodeExecutionId = nodeExecutionId,
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

    private List<WorkflowNodeDefinition?> ResolveNextNodes(string sourceId, List<WorkflowEdgeDefinition> edges,
        Dictionary<string, WorkflowNodeDefinition> nodeMap, Dictionary<string, object?> currentOutputs,
        Dictionary<string, IReadOnlyDictionary<string, object?>> allOutputs)
    {
        var result = new List<WorkflowNodeDefinition?>();
        var outEdges = edges.Where(e => e.Source == sourceId).ToList();
        foreach (var edge in outEdges)
        {
            if (string.IsNullOrEmpty(edge.Condition))
            {
                result.Add(nodeMap.GetValueOrDefault(edge.Target));
            }
            else
            {
                var scope = new Dictionary<string, object?>(currentOutputs);
                foreach (var kv2 in allOutputs) foreach (var pair in kv2.Value) scope.TryAdd(pair.Key, pair.Value);
                if (_evaluator.Evaluate(edge.Condition, scope))
                    result.Add(nodeMap.GetValueOrDefault(edge.Target));
            }
        }
        return result;
    }

    private WorkflowNodeDefinition? FindConvergenceNode(
        List<WorkflowNodeDefinition?> branchNodes,
        List<WorkflowEdgeDefinition> edges,
        Dictionary<string, WorkflowNodeDefinition> nodeMap)
    {
        var branchIds = branchNodes.Where(n => n != null).Select(n => n!.Id).ToHashSet();
        if (branchIds.Count == 0) return null;

        return edges
            .Where(e => branchIds.Contains(e.Source))
            .GroupBy(e => e.Target)
            .Where(g => branchIds.All(bid => g.Any(e => e.Source == bid)))
            .Select(g => nodeMap.GetValueOrDefault(g.Key))
            .FirstOrDefault(n => n != null);
    }

    private async Task<(NodeExecutionResult? Result, int Step)> ExecuteNodeAsync(
        WorkflowNodeDefinition current,
        Guid executionId,
        WorkflowExecution execution,
        WorkflowDefinition def,
        Dictionary<string, IReadOnlyDictionary<string, object?>> nodeOutputs,
        Dictionary<string, WorkflowNodeDefinition> nodeMap,
        Dictionary<string, object?> inputs,
        int step,
        IEngineExecutionRepository execRepo,
        RetryPolicy retryPolicy,
        IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        var node = _registry.GetNode(current.Type);
        if (node == null)
        {
            _logger.LogWarning("Node type '{NodeType}' not found during fan-out", current.Type);
            return (null, step);
        }

        step++;
        var nodeInputs = ResolveInputs(current.Id, def.Edges, nodeOutputs, inputs);
        var config = current.Config.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);

        var secretService = serviceProvider.GetService<ISecretResolver>();
        if (secretService != null)
            config = await secretService.ResolveConfigAsync(config, execution.TenantId, ct);

        var nodeExec = NodeExecution.Create(executionId, current.Id, current.Type, step);
        nodeExec.Start(JsonSerializer.Serialize(nodeInputs));
        await execRepo.CreateNodeExecutionAsync(nodeExec, ct);

        var ctx = BuildContext(executionId, execution, nodeInputs, config, nodeOutputs, inputs, step, serviceProvider, ct);
        await _notifier.NotifyNodeStarted(executionId, nodeExec.Id, current.Type, ct);

        NodeExecutionResult result;
        try { result = await node.ExecuteAsync(ctx, ct); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Node {NodeType} threw exception during fan-out", current.Type);
            result = SDK.Models.NodeExecutionResult.Failed(ex.Message);
        }

        // Retry loop
        while (result.Status == SDK.Models.NodeExecutionStatus.Failed
               && nodeExec.AttemptNumber < retryPolicy.MaxAttempts)
        {
            var delay = retryPolicy.GetDelay(nodeExec.AttemptNumber);
            nodeExec.IncrementAttempt();
            await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
            await Task.Delay(delay, ct);
            nodeExec.Start(nodeExec.InputJson);
            await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
            ctx = BuildContext(executionId, execution, nodeInputs, config, nodeOutputs, inputs, step, serviceProvider, ct);
            try { result = await node.ExecuteAsync(ctx, ct); }
            catch (Exception retryEx) { result = SDK.Models.NodeExecutionResult.Failed(retryEx.Message); }
        }

        switch (result.Status)
        {
            case SDK.Models.NodeExecutionStatus.Skipped:
                nodeExec.Skip();
                await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
                break;
            case SDK.Models.NodeExecutionStatus.Failed:
                nodeExec.Fail(result.ErrorMessage ?? "error");
                await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
                await _notifier.NotifyNodeFailed(executionId, nodeExec.Id, current.Type, result.ErrorMessage ?? "error", ct);
                break;
            default:
                nodeExec.Succeed(JsonSerializer.Serialize(result.Outputs));
                await execRepo.UpdateNodeExecutionAsync(nodeExec, ct);
                await _notifier.NotifyNodeCompleted(executionId, nodeExec.Id, current.Type, ct);
                break;
        }

        return (result, step);
    }

    private Dictionary<string, object?> ResolveInputs(string nodeId, List<WorkflowEdgeDefinition> edges,
        Dictionary<string, IReadOnlyDictionary<string, object?>> nodeOutputs, Dictionary<string, object?> workflowInputs)
    {
        var inputs = new Dictionary<string, object?>(workflowInputs);
        var matchingEdges = edges.Where(e => e.Target == nodeId).ToList();
        _logger.LogDebug("ResolveInputs for {NodeId}: {EdgeCount} incoming edges, nodeOutputs has keys: [{Keys}]",
            nodeId, matchingEdges.Count, string.Join(", ", nodeOutputs.Keys));
        foreach (var edge in matchingEdges)
        {
            if (!nodeOutputs.TryGetValue(edge.Source, out var sourceOutputs))
            {
                _logger.LogDebug("  Edge from {Source} -> source not in nodeOutputs", edge.Source);
                continue;
            }
            _logger.LogDebug("  Edge from {Source}: {Count} output keys: [{Keys}]",
                edge.Source, sourceOutputs.Count, string.Join(", ", sourceOutputs.Keys));
            if (edge.Map != null)
                foreach (var (targetKey, sourceKey) in edge.Map)
                    if (sourceOutputs.TryGetValue(sourceKey, out var mappedVal)) inputs[targetKey] = mappedVal;
            else
                foreach (var (k, v) in sourceOutputs) inputs[k] = v;
        }
        return inputs;
    }
}
