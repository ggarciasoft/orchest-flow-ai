# Agent: Workflow Engine

## Purpose
Build and maintain the workflow runtime — graph execution, state transitions, retries, pause/resume.

## Reads
- [`docs/WORKFLOW-ENGINE.md`](../docs/WORKFLOW-ENGINE.md)
- [`docs/NODE-SDK.md`](../docs/NODE-SDK.md)
- [`docs/ARCHITECTURE.md`](../docs/ARCHITECTURE.md)
- [`rules/01-general.md`](../rules/01-general.md)
- [`rules/02-backend.md`](../rules/02-backend.md)

## Write Scope
- `packages/OrchestFlowAI.Engine/`
- Engine-related use cases in `packages/OrchestFlowAI.Application/`
- Engine tests in `tests/OrchestFlowAI.Engine.Tests/`

## Responsibilities
- Implement `IWorkflowEngine` (`ValidateAsync`, `RunAsync`, `ResumeAsync`).
- Graph validation at workflow-save time (start/end nodes, no cycles, edges valid, inputs satisfied).
- Sequential node execution algorithm (MVP).
- Condition expression evaluator (safe, no dynamic eval, MVP operators only).
- Input/output mapping along edges.
- State transitions for `WorkflowExecution` and `NodeExecution`.
- Retry handling (signal retryable vs terminal; Worker schedules retries).
- Pause/resume around `human.approval` (create `ApprovalRequest`, transition to `Paused`, resume on signal).
- `WorkflowExecutionContext` construction and propagation.

## Guardrails
- The engine **must not** reference specific node implementations (only `IWorkflowNode`).
- Persistence happens through Application/Infrastructure abstractions — never direct DB calls from the engine.
- Nodes do not persist execution state; the engine does.
- No mutable shared state across concurrent executions.
- All async methods accept and respect `CancellationToken`.

## Key Types

```csharp
public interface IWorkflowEngine
{
    Task<WorkflowExecutionPlan> ValidateAsync(WorkflowDefinition def, CancellationToken ct);
    Task RunAsync(Guid executionId, CancellationToken ct);
    Task ResumeAsync(Guid executionId, ResumeSignal signal, CancellationToken ct);
}
```

## MVP Scope
- Sequential execution only.
- Conditional routing via `logic.condition`.
- Pause/resume for `human.approval`.
- No parallel, loop, or switch (Phase 10).

## Future
- `logic.parallel` fan-out/fan-in
- `logic.loop` with bounded iterations
- `logic.switch` multi-branch
- Subworkflow invocation
- Error boundaries and compensation
- Long-running durable timers
