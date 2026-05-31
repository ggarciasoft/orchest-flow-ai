# Architecture

OrchestFlowAI is a **multi-tenant AI workflow automation platform** built on a modular monorepo. It executes visual workflow graphs asynchronously, integrating LLMs, human approvals, external integrations, and structured data processing.

---

## 1. High-Level Overview

```
+-------------------------------------------------------------+
|                      Browser / Client                       |
|   Next.js 15+ (App Router)  React 19+  React Flow           |
+------------------------+------------------------------------+
                         |  HTTP / WebSocket (SignalR)
+------------------------v------------------------------------+
|                 ASP.NET Core 10 API                         |
|  Controllers -> Application (CQRS-lite) -> Domain           |
|  JWT Auth  |  RBAC Policies  |  Tenant isolation            |
+------+----------+-------------+-------------+--------------+
       |          |             |             |
  +----v--+  +---v---+  +------v------+  +---v-------+
  |  PG   |  | Redis |  | LLM Provider|  |  SMTP /   |
  |  DB   |  | Queue |  | (OpenAI...) |  |  Email    |
  +-------+  +-------+  +-------------+  +-----------+
                |
  +-------------v------------------------------------------+
  |               Workflow Execution Engine                 |
  |  WorkflowExecutionEngine  NodeRegistry  RetryPolicy     |
  |  ForEach loop  Fan-out  Human approval pause/resume     |
  +---------------------------------------------------------+
```

---

## 2. Monorepo Layout

```
orchest-flow-ai/
+-- apps/
|   +-- web/                        # Next.js 15 frontend
+-- services/
|   +-- OrchestFlowAI.Api/          # ASP.NET Core Web API + worker host
+-- packages/
|   +-- OrchestFlowAI.Domain/       # Entities, value objects, enums (no deps)
|   +-- OrchestFlowAI.Application/  # Interfaces, CQRS commands, validators
|   +-- OrchestFlowAI.Infrastructure/ # EF Core, repos, JWT, email, queues
|   +-- OrchestFlowAI.Engine/       # Workflow execution runtime
|   +-- OrchestFlowAI.Contracts/    # Shared request/response DTOs
+-- nodes/                          # Built-in node implementations
|   +-- system/
|   +-- logic/
|   +-- ai/
|   +-- integrations/
|   +-- data/
|   +-- human/
+-- docs/
+-- tests/
    +-- OrchestFlowAI.Tests/        # xUnit + Moq tests
```

**Dependency rule (strict):** `Domain` <- `Application` <- `Infrastructure` <- `Api/Engine`. Outer rings may depend on inner rings; inner rings never reference outer rings.

---

## 3. Domain Model

### Core Entities

| Entity | Tenant-scoped | Key Fields |
|--------|:-------------:|------------|
| `Tenant` | - | `Id`, `Name`, `Config` (JSON) |
| `User` | yes | `Id`, `TenantId`, `Email`, `DisplayName`, `Role`, `PasswordHash` |
| `TenantInvite` | yes | `Id`, `TenantId`, `Email`, `Role`, `Token`, `ExpiresAt`, `AcceptedAt` |
| `Workflow` | yes | `Id`, `Name`, `TriggerType`, `RetryPolicy` |
| `WorkflowVersion` | yes | `Id`, `VersionNumber`, `DefinitionJson`, `IsActive` |
| `WorkflowExecution` | yes | `Id`, `WorkflowId`, `Status`, `Input/OutputJson`, timings |
| `NodeExecution` | yes | `Id`, `ExecutionId`, `NodeId`, `Status`, `Input/OutputJson`, retries |
| `ApprovalRequest` | yes | `Id`, `ExecutionId`, correlation token, decision, comment |
| `Form` / `FormVersion` | yes | schema JSON, version history |
| `Secret` | yes | `Name`, `EncryptedValue` (AES-256-CBC), `IV` |

### Value Objects

- `TenantConfig` - execution limits, timezone, guest form fill flag
- `RetryPolicy` - max attempts, initial delay, backoff multiplier

### Enums

- `UserRole`: `Admin`, `Editor`, `Approver`, `Viewer`
- `WorkflowExecutionStatus`: `Queued`, `Running`, `Completed`, `Failed`, `Paused`, `Cancelled`
- `NodeExecutionStatus`: `Pending`, `Running`, `Completed`, `Failed`, `Skipped`
- `TriggerType`: `Manual`, `Webhook`, `Cron`

---

## 4. API Layer

All requests resolve the **tenant context from the JWT** - clients never pass `tenant_id` in the body. Cross-tenant access is impossible by construction.

### Authorization Policies

| Policy | Roles |
|--------|-------|
| `ViewerOrAbove` | Viewer, Editor, Admin, Approver |
| `EditorOrAbove` | Editor, Admin |
| `AdminOnly` | Admin |

### Key Controller Groupings

| Controller | Routes | Auth |
|------------|--------|------|
| `AuthController` | `/api/auth/login`, `/api/auth/register`, `/api/auth/me` | Public / JWT |
| `WorkflowsController` | `/api/workflows/**` | ViewerOrAbove+ |
| `ExecutionsController` | `/api/executions/**` | ViewerOrAbove+ |
| `FormsController` | `/api/forms/**` | ViewerOrAbove+ |
| `ApprovalsController` | `/api/approvals/**` | ViewerOrAbove+ |
| `TenantsController` | `/api/tenants/**` | ViewerOrAbove / AdminOnly |
| `NodesController` | `/api/nodes/catalog`, `/api/nodes/models` | ViewerOrAbove |
| `SecretsController` | `/api/secrets/**` | AdminOnly |

See [API Reference](./API.md) for full endpoint documentation.

---

## 5. Infrastructure

### Persistence

- **PostgreSQL** (EF Core 10) - primary data store
- **In-memory stubs** - zero-config development mode (no DB required; data lost on restart)
- Switch: `CONNECTION_STRING` environment variable (or `ConnectionStrings:Default` in appsettings)

### Queue

- **Redis** (StackExchange.Redis) - execution queue for distributed deployments
- **In-memory** - default when `REDIS_URL` / `Redis:Url` is not set
- All executions are enqueued; the worker service dequeues and runs them

### Email

- **`IEmailService`** - abstraction for transactional emails (invites, welcome)
- **`SmtpEmailService`** - SMTP via `System.Net.Mail`; configured via `Email:Smtp:*`
- **`LogEmailService`** - dev fallback: logs email content when SMTP is not configured
- Auto-selected at startup: SMTP when `Email:Smtp:Host` is set, otherwise log

### Auth

- **JWT** (HS256, 8-hour expiry) - issued on login and registration; also on invite acceptance
- Claims: `sub`, `email`, `ClaimTypes.Role`, `tenant_id`, `display_name`
- `JwtTokenService` generates and validates tokens

### Storage

- `IDocumentStorage` - pluggable; default: `LocalFileDocumentStorage` (`./data/uploads`)
- Tenant-prefixed paths prevent cross-tenant document access

### Encryption

- `AesEncryptionService` (AES-256-CBC) - encrypts secrets at rest
- Master key: `Encryption:MasterKey` or `ENCRYPTION_MASTER_KEY` env var

---

## 6. Workflow Definition Format

A workflow definition is a JSON object stored in `WorkflowVersion.DefinitionJson`:

```json
{
  "nodes": [
    {
      "id": "start",
      "type": "system.start",
      "position": { "x": 100, "y": 100 },
      "config": {}
    },
    {
      "id": "ai1",
      "type": "ai.llm-prompt",
      "position": { "x": 300, "y": 100 },
      "config": {
        "prompt": "Summarise the following: {{input.text}}",
        "provider": "openai",
        "model": "gpt-4o-mini",
        "outputKey": "summary"
      }
    },
    {
      "id": "end",
      "type": "system.end",
      "position": { "x": 500, "y": 100 },
      "config": {}
    }
  ],
  "edges": [
    { "id": "e1", "source": "start", "target": "ai1" },
    { "id": "e2", "source": "ai1",   "target": "end" }
  ]
}
```

- **`nodes[].id`** - unique within the definition; used as the node's key in execution state
- **`nodes[].type`** - looked up in `NodeRegistry`; determines which `INode` implementation runs
- **`nodes[].config`** - arbitrary JSON; schema defined per node type
- **`edges`** - directed graph; fan-out (one source, multiple targets) is supported
- Template strings (`{{input.key}}`, `{{nodes.nodeId.output.key}}`) are resolved at runtime by `ResolveInputs`

---

## 7. Execution Engine

The `WorkflowExecutionEngine` runs a **topological walk** over the node graph:

1. Start at `system.start`, enqueue successors
2. Pop the next ready node; build `WorkflowExecutionContext` (resolves inputs from prior outputs)
3. Call `node.ExecuteAsync(ctx)` -> `NodeExecutionResult`
4. On success: persist outputs, enqueue next nodes
5. On failure: apply `RetryPolicy` (exponential backoff); after exhaustion -> `execution.Fail()`
6. On `human.approval`: mark execution `Paused`; resume on approval/rejection via correlation token
7. On `logic.foreach`: iterate items; execute loop body for each; propagate failures + retries

**Fan-out**: multiple edges from one node - each branch runs; all must reach `system.end` for completion.

**ForEach loop**: `logic.foreach` with `loopMode` emits items; the engine runs the subgraph between the `foreach` and `logic.foreach.end` node for each item. Loop body nodes respect the same retry policy as top-level nodes.

See [Workflow Engine](./WORKFLOW-ENGINE.md) for deeper internals.

---

## 8. Multi-Tenancy

- Every entity has a `TenantId` column
- All EF queries are scoped: `WHERE tenant_id = @tenantId`
- The tenant ID comes from the JWT claim `tenant_id` - never from client input
- **Registration** creates a new isolated tenant per sign-up (each user starts as Admin of their own workspace)
- **Invitation** adds users to an existing tenant with a pre-assigned role

---

## 9. Frontend Architecture

- **Next.js App Router** - two route groups: `(auth)` (public pages) and `(app)` (authenticated)
- **`(app)/layout.tsx`** - authentication guard + sidebar navigation; role-filtered nav items
- **`AuthContext`** - React Context providing `role`, `canEdit`, `isAdmin`, `isApprover` derived from the JWT; hydrated client-side from `localStorage`
- **`apiFetch`** - central HTTP client; injects `Authorization: Bearer <token>`; auto-redirects to `/login` on 401
- **TanStack Query** - all server state (lists, details, mutations); `queryKey` patterns: `['workflows']`, `['executions', id]`, etc.

See [Frontend](./FRONTEND.md) for screen-level documentation.

---

## 10. Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Single deployable | API + Worker in one process | Simplifies deployment for MVP; split is trivial later |
| Stub repositories | In-memory ConcurrentDictionary | Zero-config dev; test isolation without DB containers |
| Email abstraction | `IEmailService` + `LogEmailService` fallback | Transactional email works in dev without SMTP config |
| JWT in `localStorage` | Accept XSS risk for dev simplicity | Move to HttpOnly cookie before GA |
| SHA-256 passwords | Simple for MVP | Replace with BCrypt/Argon2 before user-facing launch |
| ForEach loop mode | Subgraph execution via `_foreach_items` | Enables per-item retry, failure isolation, flexible branching |

---

## 11. ADR Index

Architecture Decision Records live in `docs/adr/`. None have been formally recorded yet - decisions above represent current state.

---

## 12. Related Documentation

- [API Reference](./API.md)
- [Workflow Engine](./WORKFLOW-ENGINE.md)
- [Nodes Reference](./NODES.md)
- [Database](./DATABASE.md)
- [Security](./SECURITY.md)
- [Frontend](./FRONTEND.md)
