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

---

## 12. Workflow Trigger Modes

Every workflow has a TriggerType field that determines how executions are started.

### 12.1 Manual

TriggerType = Manual (default). Executions are started explicitly:
- Via POST /api/workflows/{id}/execute (authenticated, tenant-scoped).
- No additional configuration required.

### 12.2 Webhook

TriggerType = Webhook. An inbound HTTP call starts the execution:
- Workflow must have a WebhookSecret (set via SetTrigger(Webhook, secret, null)).
- Caller sends POST /api/webhooks/{workflowId} with header X-Webhook-Secret: <secret>.
- **Responses:**
  - 202 Accepted + { "executionId": "..." } � enqueued successfully.
  - 401 Unauthorized � missing or wrong secret.
  - 404 Not Found � workflow not found or not a Webhook-type workflow.
- The endpoint is intentionally **unauthenticated** � verification relies solely on the shared secret.
- WebhookSecret is never included in log output (security rule 6).

### 12.3 Cron / Schedule

TriggerType = Cron. Executions run on a recurring schedule:
- Workflow must have a CronExpression (standard 5-part cron, parsed by [Cronos](https://github.com/HangfireIO/Cronos)).
- CronSchedulerService (Worker) evaluates all cron workflows every 60 seconds.
- If the next scheduled occurrence has passed and the workflow was not already triggered within this minute, an execution is enqueued.
- Last-triggered time is tracked in memory (per Worker process). No DB tracking for MVP.
- Example expressions:
  -   9 * * 1-5 � every weekday at 09:00 UTC.
  - */5 * * * * � every 5 minutes.
  -   0 1 * * � midnight on the first of every month.

### Setting the Trigger via API

The CreateWorkflowRequest DTO accepts TriggerType, WebhookSecret, and CronExpression fields.
Trigger settings can also be updated via SetTrigger(...) on the domain entity:

`csharp
workflow.SetTrigger(TriggerType.Webhook, webhookSecret: "my-secret", cronExpression: null);
workflow.SetTrigger(TriggerType.Cron, webhookSecret: null, cronExpression: "0 * * * *");
workflow.SetTrigger(TriggerType.Manual, webhookSecret: null, cronExpression: null);
`

Guard conditions: Webhook requires a non-empty WebhookSecret; Cron requires a non-empty CronExpression.
---

## 13. Execution Retry / Exponential Backoff

Workflows support automatic retry of failed node executions with exponential backoff.

### Configuration

Set a RetryPolicy on the workflow via the API or domain entity:

`json
{
  "name": "My Workflow",
  "retryMaxAttempts": 3,
  "retryBackoffMs": 500,
  "retryBackoffMultiplier": 2.0
}
`

Or in code:

`csharp
workflow.SetRetryPolicy(RetryPolicy.Create(maxAttempts: 3, backoffMs: 500, multiplier: 2.0));
`

### Retry Policy Fields

| Field | Default | Description |
|-------|---------|-------------|
| MaxAttempts | 0 | Maximum number of execution attempts. 0 = no retry. |
| BackoffMs | 0 | Base delay in milliseconds before the first retry. |
| BackoffMultiplier | 2.0 | Multiplier applied to delay on each subsequent attempt. |

### Delay Calculation

`
delay = BackoffMs * (BackoffMultiplier ^ (attemptNumber - 1))
`

| Attempt | Delay (500ms base, 2x multiplier) |
|---------|-----------------------------------|
| 1 (first retry) | 500ms |
| 2 | 1000ms |
| 3 | 2000ms |

### Engine Behavior

1. On node failure, the engine checks if AttemptNumber < RetryPolicy.MaxAttempts.
2. If yes: waits the calculated delay, increments AttemptNumber on the NodeExecution, and retries.
3. If no (retries exhausted): marks NodeExecution as Failed and fails the WorkflowExecution.
4. Each retry attempt is logged with LogWarning including the attempt number and delay.

### RetryPolicy.None

The default is RetryPolicy.None (MaxAttempts = 0) � no retry, fail immediately on error.
