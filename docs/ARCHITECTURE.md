# Architecture

This document explains how OrchestFlowAI is structured, why each piece exists, and how data flows through the system.

---

## 1. System Diagram (Logical)

```
                       ┌─────────────────────────────────────┐
                       │           Web App (Next.js)         │
                       │  Designer · Dashboard · Approvals    │
                       └─────────────────┬───────────────────┘
                                         │ HTTPS (REST)
                                         ▼
                       ┌─────────────────────────────────────┐
                       │         OrchestFlowAI.Api (.NET)        │
                       │  Workflows · Executions · Approvals  │
                       │  Documents · Node Catalog · Auth     │
                       └────────┬─────────────────┬──────────┘
                                │                 │
                  enqueue       │                 │  read/write
                                ▼                 ▼
                       ┌──────────────┐    ┌──────────────────┐
                       │   Queue/Bus  │    │   PostgreSQL     │
                       │  (Redis/RMQ) │    │   (Domain DB)    │
                       └──────┬───────┘    └──────────────────┘
                              │
                              ▼
                       ┌─────────────────────────────────────┐
                       │       OrchestFlowAI.Worker (.NET)        │
                       │   Engine · Node Registry · Retries  │
                       │   Pause/Resume · State Persistence  │
                       └────────┬──────────────────┬────────┘
                                │                  │
                                ▼                  ▼
                       ┌──────────────┐    ┌──────────────────┐
                       │  AI Runtime  │    │ Integrations /   │
                       │ (LLM Abstr.) │    │ Connectors       │
                       └──────┬───────┘    └──────────────────┘
                              │
                              ▼
                       ┌──────────────────────────────────────┐
                       │   LLM Providers (OpenAI, Azure,      │
                       │   Anthropic, local, …)               │
                       └──────────────────────────────────────┘
```

---

## 2. Layered Architecture (Clean Architecture)

```
┌──────────────────────────────────────────────────────────────┐
│ Apps                  apps/web, apps/docs                    │
├──────────────────────────────────────────────────────────────┤
│ Services              Api · Worker · AI                      │  ← composition roots
├──────────────────────────────────────────────────────────────┤
│ Infrastructure        Postgres · Redis · LLM clients · Files │  ← adapters
├──────────────────────────────────────────────────────────────┤
│ Application           Use cases · Orchestration · Engine     │  ← business orchestration
├──────────────────────────────────────────────────────────────┤
│ Domain                Entities · Value objects · Rules       │  ← pure, no I/O
└──────────────────────────────────────────────────────────────┘
                          ▲ Contracts · SDK · Observability (cross-cutting)
```

**Rules**

- Domain has zero infrastructure dependencies.
- Application depends only on Domain + Contracts + abstractions.
- Infrastructure implements abstractions defined in Application/Domain.
- Services (Api/Worker) are thin composition roots; they wire DI and host HTTP/loops.

---

## 3. Projects and Responsibilities

### `services/OrchestFlowAI.Api`

REST API consumed by the web app.

Responsibilities:

- Workflow management (CRUD)
- Workflow version management
- Execution triggering
- Execution history retrieval
- Approval actions (approve/reject)
- Node metadata exposure
- Document upload endpoints
- Auth & authorization entry point

Endpoint catalog → [`API.md`](./API.md).

### `services/OrchestFlowAI.Worker`

Background workflow execution service.

Responsibilities:

- Pick up pending workflow executions from queue/DB.
- Execute nodes through the engine.
- Persist node execution state.
- Handle retries with backoff.
- Pause workflows awaiting human approval.
- Resume workflows when approvals arrive.
- Publish execution events.

### `services/OrchestFlowAI.AI`

AI runtime — may start as a module inside Worker, can graduate to its own service.

Responsibilities:

- LLM provider abstraction
- Prompt execution
- Structured-output handling
- Token & cost tracking
- (Future) Tool calling
- (Future) RAG integration

Detail → [`AI-RUNTIME.md`](./AI-RUNTIME.md).

### `packages/OrchestFlowAI.Engine`

Core workflow runtime.

Responsibilities:

- Load and validate workflow definition (graph).
- Resolve executable nodes.
- Manage `WorkflowExecutionContext`.
- Route outputs from one node to the next.
- Evaluate edge conditions.
- Handle terminal states.

Detail → [`WORKFLOW-ENGINE.md`](./WORKFLOW-ENGINE.md).

### `packages/OrchestFlowAI.SDK`

Developer SDK for creating nodes.

Responsibilities:

- `IWorkflowNode` interface
- `IWorkflowNodeDescriptor` model
- Input/output definitions
- Configuration schema definitions
- Execution contracts

Detail → [`NODE-SDK.md`](./NODE-SDK.md).

### `packages/OrchestFlowAI.Contracts`

Shared DTOs, events, API request/response models, enums. Pure POCOs.

### `packages/OrchestFlowAI.Domain`

Pure domain entities and rules. Entities include:

- `Workflow`, `WorkflowVersion`, `WorkflowNode`, `WorkflowEdge`
- `WorkflowExecution`, `NodeExecution`
- `ApprovalRequest`
- `Document`
- `AIUsageLog`, `AuditLog`
- `Tenant`, `User`

### `packages/OrchestFlowAI.Application`

Use cases (CQRS-style commands/queries). Owns orchestration around Domain + Engine.

### `packages/OrchestFlowAI.Infrastructure`

Concrete adapters:

- PostgreSQL via EF Core or Dapper
- Redis (cache, optional queue)
- File/document storage (local fs in MVP; S3-compatible later)
- AI provider clients
- Email provider (post-MVP)
- Logging/tracing adapters

### `packages/OrchestFlowAI.Observability`

Cross-cutting observability helpers — correlation IDs, OpenTelemetry config, structured logging helpers, AI usage tracking abstractions.

### `nodes/*`

Node implementations grouped by category. Each node is a small project (or folder) that depends on `OrchestFlowAI.SDK`.

### `apps/web`

Next.js 14+ application. Pages, the workflow designer (React Flow), the dashboard, approval inbox, execution timeline. Detail → [`FRONTEND.md`](./FRONTEND.md).

---

## 4. Execution Lifecycle

```
1. User clicks "Execute" in the designer (or trigger fires).
2. Api validates the request, creates a WorkflowExecution row (status=Queued),
   stores input JSON, and enqueues an execution message.
3. Worker picks up the message, loads the workflow definition + version.
4. Engine validates the graph and walks nodes from system.start.
5. For each node:
   a. Resolve inputs from previous node outputs / workflow inputs.
   b. Update NodeExecution to Running, persist start time.
   c. Call IWorkflowNode.ExecuteAsync(context, ct).
   d. Persist outputs / error / status.
   e. If status == WaitingForApproval:
       - Create ApprovalRequest.
       - Set WorkflowExecution to Paused.
       - Worker stops; execution resumes when an approval API call enqueues a resume.
   f. Otherwise route to the next node based on edges / conditions.
6. When system.end runs (or all branches finish), WorkflowExecution → Completed/Failed.
7. Events are emitted (execution started/completed/failed; approval requested).
```

---

## 5. Multi-Tenancy Model

- Every domain row carries `tenant_id`.
- All queries filter by tenant. This is enforced at the repository layer with a `TenantContext` injected per request.
- File storage paths are tenant-prefixed.
- API auth resolves a tenant from the user; cross-tenant access is impossible at the data layer.

---

## 6. Workflow Definition Shape

A workflow definition is canonical JSON:

```json
{
  "id": "contract-review-v1",
  "name": "Contract Review Workflow",
  "version": 1,
  "nodes": [
    { "id": "start", "type": "system.start", "position": { "x": 100, "y": 100 }, "config": {} },
    { "id": "extractPdf", "type": "document.extract-pdf-text", "position": { "x": 350, "y": 100 }, "config": {} },
    { "id": "analyzeRisk", "type": "ai.contract-risk-analysis", "position": { "x": 600, "y": 100 }, "config": { "model": "default", "riskThreshold": "high" } },
    { "id": "approval", "type": "human.approval", "position": { "x": 850, "y": 100 }, "config": { "requiredWhen": "riskLevel == 'High'" } },
    { "id": "end", "type": "system.end", "position": { "x": 1100, "y": 100 }, "config": {} }
  ],
  "edges": [
    { "source": "start", "target": "extractPdf" },
    { "source": "extractPdf", "target": "analyzeRisk" },
    { "source": "analyzeRisk", "target": "approval" },
    { "source": "approval", "target": "end" }
  ]
}
```

Conditional edges are supported via an optional `condition` field (e.g. `"riskLevel == 'High'"`).

---

## 7. Cross-Cutting Concerns

| Concern         | Lives in                  | Notes                                              |
| --------------- | ------------------------- | -------------------------------------------------- |
| Auth            | `Api` (middleware)        | JWT/cookie; tenant resolution per request          |
| Authorization   | `Application` (policies)  | Roles/policies enforced in use cases               |
| Tenancy         | `Application` + `Infra`   | `TenantContext`; repository filters                |
| Logging         | `Observability`           | Structured (Serilog), correlation IDs              |
| Tracing         | `Observability`           | OpenTelemetry (post-MVP minimum)                   |
| Metrics         | `Observability`           | Prometheus-compatible counters/histograms          |
| Secrets         | `Infrastructure`          | Env vars / vault; never in workflow JSON           |
| Validation      | `Application`             | FluentValidation for commands; schema for workflows |
| Error handling  | All layers                | Domain errors → typed; infra errors → wrapped       |

---

## 8. Decision Records

Significant architectural decisions live in `docs/adr/NNNN-title.md` (to be created on demand). Use ADRs for tradeoffs like:

- Choice of EF Core vs Dapper
- Queue backend (Redis Streams vs RabbitMQ vs Postgres LISTEN/NOTIFY)

---

## 9. Persistent Execution Queue

The persistent queue is backed by the `ExecutionQueue` table in PostgreSQL and accessed via `IPersistentExecutionQueue`.

### Key design decisions

| Concern | Decision |
|---------|----------|
| Atomicity | `SELECT FOR UPDATE SKIP LOCKED` ensures each worker claims exactly one item, even under concurrent load |
| Ordering | Items are dequeued by `CreatedAt ASC` (FIFO within the same priority) |
| Fault tolerance | Worker marks items `Processing` on pickup; items stuck in `Processing` can be re-queued by a watchdog (future work) |
| Fallback | `StubExecutionQueue` (in-memory `ConcurrentQueue`) is used when `CONNECTION_STRING` is absent (local dev / tests) |

### Status lifecycle
```
Pending → Processing → Done
                    ↘ Failed
```

### Registration
- `CONNECTION_STRING` set → `PostgresExecutionQueue` registered as `IPersistentExecutionQueue` (scoped, depends on `OrchestFlowAIDbContext`)
- No `CONNECTION_STRING` → `StubExecutionQueue` registered as `IPersistentExecutionQueue` (singleton)

### Table schema
```sql
CREATE TABLE "ExecutionQueue" (
    "Id"          uuid         PRIMARY KEY,
    "WorkflowId"  uuid         NOT NULL,
    "TenantId"    uuid         NOT NULL,
    "TriggeredBy" varchar(50)  NOT NULL,
    "Payload"     text         NOT NULL,
    "Status"      varchar(20)  NOT NULL,
    "CreatedAt"   timestamptz  NOT NULL,
    "PickedUpAt"  timestamptz,
    "CompletedAt" timestamptz
);
CREATE INDEX ON "ExecutionQueue" ("Status", "CreatedAt");
CREATE INDEX ON "ExecutionQueue" ("TenantId");
```
- Multi-tenant strategy (row-level vs schema-per-tenant)
- Workflow versioning strategy
- Engine durability model


---

## 10. Dynamic Form Node Registry & Hot-Reload

Custom forms (`form.<slug>` node types) are user-defined at runtime. The engine must know about them before it can execute a workflow containing a form node.

### How form nodes are registered

| Process | Mechanism |
|---------|-----------|
| **API** (`OrchestFlowAI.Api`) | `FormNodeRegistrar` (hosted service) loads all forms at startup and re-registers after every create/update/delete in `FormsController`. Deleted forms are unregistered immediately. |
| **Worker** (`OrchestFlowAI.Worker`) | `WorkerFormNodeRegistrar` (background service) loads all forms at startup, then **polls the database every 30 seconds** to sync additions, updates, and deletions. |

### Hot-reload lifecycle

```
Form created/updated/deleted
        �
        ?
API: FormNodeRegistrar.RefreshAsync()    ? immediate (synchronous after DB write)
        �
        ?
Worker: WorkerFormNodeRegistrar polls    ? within 30 s (configurable via WorkerFormNodeRegistrar.RefreshIntervalSeconds)
```

### Key points

- Both API and Worker share the same singleton `NodeRegistry` within their own process.
- `NodeRegistry` uses `ConcurrentDictionary` � registration and lookup are thread-safe.
- Re-registering an existing type (`Register` overwrites) is safe and idempotent.
- Removed forms are unregistered via `Unregister(type)` � stale entries are cleaned from both nodes and descriptors dictionaries.
- The 30-second polling interval means a workflow execution that starts within 30 s of a new form being created **may** still get "node type not found". This window is acceptable for typical usage.