# OrchestAI — Known Gaps & Work Backlog

> Created: 2026-05-23  
> Last updated: 2026-05-24

---

## ✅ Resolved

| Item | Resolution |
|---|---|
| No real database — in-memory only | PostgreSQL + EF Core wired; `OrchestAIDbContext`; auto-migration on startup |
| Designer Save not implemented | `POST /api/workflows/{id}/versions` wired to canvas serialization |
| JWT auth not persisted in frontend | Auth guard on `(app)/layout.tsx`; `localStorage` token verified |
| AI nodes require real API key | `LLM_PROVIDER=fake` dev fallback; `.env.example` documented |
| Workflow definition not loaded into designer | Fetches active version on mount, hydrates ReactFlow canvas |
| Approval detail page missing | `/approvals/[id]` page with timeline |
| Swagger not enabled in all envs | Swagger + JWT auth button shown in all environments |
| Docker Compose all-in-one | Split into `docker-compose.yml` (infra) / `docker-compose.app.yml` / `docker-compose.observability.yml` |
| HTTP node — only one auth mode | 5 auth types: `none`, `bearer`, `basic`, `api-key`, `oauth2-client-credentials` |
| No reusable node config system | Node Config Presets — full CRUD backend + frontend `/settings/presets` |
| No Workflow Trigger Types | `TriggerType` enum (Manual/Webhook/Cron) added to domain; `POST /api/webhooks/{id}` endpoint; `CronSchedulerService` background service using Cronos |

---

## 🔴 Critical Gaps (Blocking End-to-End Execution)

### 1. Workflow Execution Queue is In-Memory
- **Problem:** `InMemoryExecutionQueue` uses .NET Channels. Doesn't survive restarts, no horizontal scaling.
- **Fix (future):** RabbitMQ / Azure Service Bus / Redis Streams. In-memory is acceptable for single-process MVP.

---

## 🟡 Important Gaps (Partial Functionality)

### 3. No Execution Retry / Backoff
- **Problem:** If a node fails (e.g. HTTP 503), the whole execution fails. There's no automatic retry.
- **Proposed fix:** `NodeConfigDefinition` already supports `RetryPolicy`-style config. Add `RetryAttempts` + `RetryBackoffMs` config fields to the engine and honor them in `WorkflowEngine`.

### 4. No Real-Time Execution Log Streaming
- **Problem:** Execution status only shows on polling refresh. No live node-by-node updates.
- **Proposed fix:** SignalR hub or SSE endpoint streaming `NodeExecution` updates as they happen.

### 5. No Role-Based Access Control (RBAC)
- **Problem:** All authenticated users have full access to all workflows/executions in the tenant. No per-workflow permissions, no "viewer" vs "editor" vs "admin" distinction.
- **Proposed fix:** Add `Role` field to `User`; guard controller endpoints with role claims.

### 6. Tenant Onboarding Flow Missing
- **Problem:** There's no UI or guided flow for creating a new tenant or inviting team members.
- **Proposed fix:** `/onboarding` page and `POST /api/tenants` + `POST /api/tenants/{id}/invite` endpoints.

---

## 🟢 Working (Verified)

| Feature | Status |
|---|---|
| User login (JWT) | ✅ |
| Workflow create / list / save | ✅ |
| Node registry + catalog (19 nodes) | ✅ |
| Designer drag, save, load, delete | ✅ |
| Execution engine (node graph walking) | ✅ |
| Execution enqueue + worker pickup | ✅ in-memory |
| Approval inbox + detail page | ✅ |
| HTTP node — all 5 auth types | ✅ |
| Node Config Presets (backend + frontend) | ✅ |
| PostgreSQL + EF Core + migrations | ✅ |
| Auto-migrate on startup | ✅ |
| Swagger + JWT auth button | ✅ all envs |
| Docker Compose split (3 files) | ✅ |
| Backend unit tests | ✅ **173 / 173** |
| Frontend unit tests | ✅ **29 / 29** |
| XML / JSDoc docs on all public APIs | ✅ |

---

## Suggested Priority Order

1. **Workflow trigger types** — webhook + cron triggers unlock real automation
2. **Execution retry / backoff** — makes integrations production-safe
3. **Real-time execution log streaming** — major UX improvement for debugging
4. **RBAC** — required before multi-user teams can safely share a tenant
5. **Tenant onboarding UI** — needed for self-serve sign-up
6. **Replace in-memory queue** — production readiness only

---

_Update this file as items are resolved._
