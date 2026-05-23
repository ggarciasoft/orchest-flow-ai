# OrchestAI — Known Gaps & Work Backlog

> Created: 2026-05-23  
> Status: Items below are **not yet implemented** and block full end-to-end workflow execution.

---

## 🔴 Critical Gaps (Blocking End-to-End Execution)

### 1. No Real Database — In-Memory Only
- **Problem:** All repositories (`StubWorkflowRepository`, `StubExecutionRepository`, etc.) use `ConcurrentDictionary` in RAM. All data is lost on every server restart.
- **Fix:** Wire up PostgreSQL + Entity Framework Core. Create EF Core `DbContext`, implement real repository classes, write and run migrations.
- **Files to change:**
  - `packages/OrchestAI.Infrastructure/Repositories/StubRepositories.cs` → replace with real EF implementations
  - Add `OrchestAI.Infrastructure/Persistence/OrchestAIDbContext.cs`
  - Add `migrations/` folder
  - Update `services/OrchestAI.Api/Program.cs` to register real DB context

---

### 2. Designer Save Button — Not Implemented
- **Problem:** The Save button in `WorkflowDesigner.tsx` shows a placeholder `alert('Save workflow coming in next phase!')`. Node/edge layouts designed in the UI are **never persisted**.
- **Fix:** Serialize current ReactFlow `nodes` + `edges` into the engine's `WorkflowDefinition` format and call `POST /api/workflows/{id}/versions`, then optionally activate the new version.
- **Files to change:**
  - `apps/web/src/components/designer/WorkflowDesigner.tsx` — implement `handleSave()`
  - `apps/web/src/lib/api.ts` — add `api.workflows.saveVersion(id, definition)`
  - Add unit test for save flow

---

### 3. JWT Auth Not Persisted in Frontend
- **Problem:** The login page calls `setToken(token)` but the `api.ts` client reads from `localStorage`. Need to verify the token is properly stored and sent on all requests, especially after page refresh.
- **Fix:** Confirm `setToken` writes to `localStorage` and that `apiFetch` reads it correctly. Add an auth guard/middleware to redirect unauthenticated users.
- **Files to check:**
  - `apps/web/src/lib/auth.ts`
  - `apps/web/src/lib/api.ts`
  - `apps/web/src/app/(app)/layout.tsx` — add auth guard

---

## 🟡 Important Gaps (Partial Functionality)

### 4. AI Nodes Require Real OpenAI API Key
- **Problem:** `ContractRiskAnalysisNode` and `ExecutiveSummaryNode` call `OpenAILLMProvider` which requires a valid `OPENAI_API_KEY`. Without it, AI node execution will fail at runtime.
- **Fix:** Document required env vars. Add fallback to `FakeLLMProvider` in dev/local mode via config flag.
- **Files to change:**
  - `services/OrchestAI.Api/Program.cs` — env-based provider selection
  - Add `.env.example` at repo root

---

### 5. Workflow Execution Queue is In-Memory
- **Problem:** `InMemoryExecutionQueue` uses .NET Channels. Works within a single process but doesn't survive restarts and doesn't support horizontal scaling.
- **Fix (future):** Replace with a real message broker (RabbitMQ, Azure Service Bus, or Redis Streams) for production use. For MVP, in-memory is acceptable **only if** the server stays running.

---

### 6. Workflow Definition Not Loaded into Designer
- **Problem:** When opening an existing workflow in the designer (`/workflows/{id}/designer`), the canvas starts empty. The existing definition (nodes/edges) from the active version is never fetched and rendered.
- **Fix:** On designer mount, fetch `GET /api/workflows/{id}` + active version definition and hydrate the ReactFlow canvas.
- **Files to change:**
  - `apps/web/src/app/(app)/workflows/[id]/designer/page.tsx`
  - `apps/web/src/components/designer/WorkflowDesigner.tsx` — accept `initialDefinition` prop

---

### 7. No Approval Workflow Page for Specific Execution
- **Problem:** The approval inbox shows pending approvals but clicking one doesn't link to the specific execution context.
- **Fix:** Add a detail view per approval with execution timeline context.

---

## 🟢 Working (Verified)

| Feature | Status |
|---|---|
| User login (JWT) | ✅ Works |
| Workflow create (name + description) | ✅ Works |
| Workflow list | ✅ Works |
| Node registry + catalog | ✅ Works |
| Drag nodes onto canvas | ✅ Works |
| Delete nodes (keyboard / right-click / drawer) | ✅ Works |
| Execution engine logic (node graph walking) | ✅ Works (in-process) |
| Execution enqueue + worker pickup | ✅ Works (in-memory, same process) |
| Approval approve/reject API | ✅ Works |
| All backend unit tests | ✅ 88/88 passing |
| All frontend unit tests | ✅ 24/24 passing |

---

## Suggested Priority Order

1. **Implement Save in Designer** — quickest win, unblocks real workflow design
2. **Load existing definition into Designer** — needed to edit saved workflows
3. **Wire up PostgreSQL** — needed for persistence across restarts
4. **Auth guard on frontend** — prevent unauthenticated access
5. **OpenAI key config + fallback** — needed to test AI nodes end-to-end
6. **Replace in-memory queue** — production readiness only

---

_Update this file as items are resolved._
