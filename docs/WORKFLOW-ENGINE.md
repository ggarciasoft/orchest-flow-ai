# Workflow Engine

The engine is the heart of OrchestAI. It loads a workflow definition, validates it, and executes nodes through a uniform interface while persisting state at every step.

---

## 1. Responsibilities

| Concern                     | Owner            |
| --------------------------- | ---------------- |
| Graph validation            | Engine           |
| Topological ordering        | Engine           |
| Node execution dispatch     | Engine           |
| Input/output routing        | Engine           |
| State transitions           | Engine           |
| Retry policy                | Engine + Worker  |
| Pause/resume around approval | Engine + Worker |
| Persistence                 | Application/Infra |
| Provider-specific work      | Individual nodes |

**Nodes do not** persist execution state. **The engine does.**

---

## 2. Core Types

### `WorkflowExecutionContext`

The runtime context passed to each node.

```csharp
public sealed class WorkflowExecutionContext
{
    public Guid ExecutionId { get; init; }
    public Guid WorkflowId { get; init; }
    public Guid WorkflowVersionId { get; init; }
    public Guid TenantId { get; init; }
    public Guid? TriggeredByUserId { get; init; }
    public string CorrelationId { get; init; } = default!;

    // Inputs the workflow was started with.
    public IReadOnlyDictionary<string, object?> WorkflowInputs { get; init; } = default!;

    // Per-node outputs collected so far, keyed by node id.
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> NodeOutputs { get; init; } = default!;

    // Logical clock; useful for ordering and observability.
    public int Step { get; init; }

    // Services accessible to nodes (DI scope).
    public IServiceProvider Services { get; init; } = default!;

    // Cancellation propagated from worker.
    public CancellationToken CancellationToken { get; init; }
}
```

Nodes resolve inputs by reading `NodeOutputs[sourceNodeId][outputKey]` or `WorkflowInputs[key]` — but in practice the engine performs input mapping based on edges and exposes a typed view per node.

### `NodeExecutionResult`

```csharp
public sealed class NodeExecutionResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object?> Outputs { get; init; } = new();
    public NodeExecutionStatus Status { get; init; }
}

public enum NodeExecutionStatus
{
    Succeeded,
    Failed,
    WaitingForApproval,
    Skipped,
    Cancelled
}
```

### `IWorkflowNode`

```csharp
public interface IWorkflowNode
{
    string Type { get; }

    Task<NodeExecutionResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken);
}
```

---

## 3. State Machine

### Workflow Execution

```
Queued ──► Running ──► Completed
                ├──► Failed
                └──► Paused ──► Running ──► Completed/Failed
                                └─► Cancelled
```

### Node Execution

```
Pending ──► Running ──► Succeeded
              ├──► Failed (retryable or terminal)
              ├──► WaitingForApproval (parks the workflow)
              ├──► Skipped (condition false)
              └──► Cancelled
```

---

## 4. Execution Algorithm (MVP)

The MVP supports sequential and conditional flows. Parallelism, loops, and switches arrive in Phase 10.

```text
LoadDefinition(executionId)
ValidateGraph()
current ← node where type == "system.start"

while current is not null:
    ctx ← BuildContext(current)
    record NodeExecution(Running, started_at)
    try:
        result ← current.ExecuteAsync(ctx, ct)
    catch (Exception ex):
        result ← Failed(ex)

    persist NodeExecution(result)

    switch result.Status:
        case Succeeded:
            current ← ResolveNext(current, result.Outputs)
        case Skipped:
            current ← ResolveNext(current, result.Outputs)
        case WaitingForApproval:
            persist WorkflowExecution(Paused)
            create ApprovalRequest
            return  // worker yields; resume on approval
        case Failed:
            if retryable && retries < policy.max:
                schedule retry with backoff
                return
            persist WorkflowExecution(Failed)
            return
        case Cancelled:
            persist WorkflowExecution(Cancelled)
            return

persist WorkflowExecution(Completed)
emit ExecutionCompleted
```

`ResolveNext` evaluates the outgoing edges of `current`. If an edge has a `condition`, the engine evaluates it against `result.Outputs` (and accumulated context). The first edge whose condition is true (or has no condition) is followed.

### Condition Expressions

For the MVP, conditions are simple boolean expressions:

- Operators: `==`, `!=`, `>`, `<`, `>=`, `<=`, `&&`, `||`, `!`
- Operands: literals (`'High'`, `42`, `true`) or node-output references (`riskLevel`, `score`)
- No function calls, no I/O

A safe expression evaluator is implemented inside the engine; no `eval`-like dynamic compilation.

---

## 5. Input / Output Mapping

Each edge may declare an explicit mapping:

```json
{
  "source": "analyzeRisk",
  "target": "approval",
  "map": {
    "riskLevel": "riskLevel",
    "summary": "summary"
  }
}
```

If `map` is omitted, the engine passes all of the source node's outputs to the target node's input scope.

Nodes declare their inputs/outputs via their descriptor. The engine validates at workflow-save time that:

- Every required input on a downstream node has either a literal value, a configured value, or a mapped output from an upstream node.
- Output keys exist on the source node descriptor.

---

## 6. Retry Policy

MVP retry policy is fixed per node category and overridable via node `config.retry`:

```json
{
  "retry": {
    "maxAttempts": 3,
    "backoff": "exponential",
    "initialDelayMs": 1000,
    "maxDelayMs": 30000
  }
}
```

Retryable errors:

- Transient HTTP errors (5xx, 408, 429 with `Retry-After`)
- LLM provider rate limits
- Database transient failures

Non-retryable (terminal) errors:

- Validation errors
- Authorization errors
- Schema mismatches in structured AI output (after one self-correction pass)

The Worker is responsible for scheduling retries (visibility timeout / delayed message). The engine only signals failure + retryability.

---

## 7. Pause and Resume

When a node returns `WaitingForApproval`:

1. The engine persists the node execution with `status=WaitingForApproval`.
2. An `ApprovalRequest` row is created with the approval payload (risk summary, AI recommendation, etc.).
3. The workflow execution transitions to `Paused`.
4. The Worker exits the execution loop for this workflow.

When an approval API call arrives (`POST /api/approvals/{id}/approve|reject`):

1. The Api persists the decision.
2. The Api enqueues a resume message for the workflow execution.
3. The Worker picks it up, sets the approval node's outputs (`{ decision, comment, decidedAt, decidedBy }`), marks the node as `Succeeded`, and continues the algorithm at step `ResolveNext`.

Idempotency: the resume message includes the approval id; the Worker uses it as the idempotency key.

---

## 8. Validation Rules

At workflow save time (`POST /api/workflows`), the engine validates the definition:

- Exactly one `system.start` node.
- At least one `system.end` node.
- All node `type` values exist in the node registry.
- All edges reference existing node ids.
- No cycles (MVP). Loops arrive with `logic.loop` later.
- Required configuration fields are present on every node.
- Edge conditions parse successfully.
- Required inputs on every downstream node are satisfied.
- Output keys referenced in mappings exist on source descriptors.

Validation errors return structured details so the frontend can highlight nodes/edges.

---

## 9. Concurrency Model

- A workflow execution runs single-threaded inside the worker (sequential MVP).
- Multiple **different** executions run concurrently across worker processes.
- The Worker uses a queue with visibility timeout + lease renewal to ensure exactly-once semantics for state transitions.
- Database writes are wrapped in transactions per node-execution boundary.

Parallel nodes (Phase 10) will use structured fan-out/fan-in primitives, not arbitrary threading inside node code.

---

## 10. Engine Public API (within process)

```csharp
public interface IWorkflowEngine
{
    Task<WorkflowExecutionPlan> ValidateAsync(WorkflowDefinition def, CancellationToken ct);
    Task RunAsync(Guid executionId, CancellationToken ct);
    Task ResumeAsync(Guid executionId, ResumeSignal signal, CancellationToken ct);
}

public sealed record ResumeSignal(
    Guid NodeExecutionId,
    IReadOnlyDictionary<string, object?> ResumeOutputs);
```

The Worker uses `RunAsync` for new executions and `ResumeAsync` after approvals.

---

## 11. Future Engine Features

- Parallel and fan-out/fan-in nodes (`logic.parallel`)
- Loops (`logic.loop`) with bounded iterations
- Switch (`logic.switch`) for multi-branch routing
- Subworkflows (a node that invokes another workflow)
- Error boundaries / compensation (`system.error-boundary`, `system.compensation-step`)
- Long-running timers (`logic.delay` with durable timer)
- Workflow-level retry & SLAs
- Hot-reload of node implementations
