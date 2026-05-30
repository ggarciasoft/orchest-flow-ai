# OrchestFlowAI — Known Gaps & Work Backlog

> Created: 2026-05-23
> Last updated: 2026-05-30

---

## ✅ Resolved

| Item | Resolution |
|---|---|
| No real database — in-memory only | PostgreSQL + EF Core wired; `OrchestFlowAIDbContext`; auto-migration on startup |
| Designer Save not implemented | `POST /api/workflows/{id}/versions` wired to canvas serialization |
| JWT auth not persisted in frontend | Auth guard on `(app)/layout.tsx`; `localStorage` token verified |
| AI nodes require real API key | `LLM_PROVIDER=fake` dev fallback; `.env.example` documented |
| Workflow definition not loaded into designer | Fetches active version on mount, hydrates ReactFlow canvas |
| Approval detail page missing | `/approvals/[id]` page with timeline |
| Swagger not enabled in all envs | Swagger + JWT auth button shown in all environments |
| Docker Compose all-in-one | Split into `docker-compose.yml` (infra) / `docker-compose.app.yml` / `docker-compose.observability.yml` |
| HTTP node — only one auth mode | 5 auth types: `none`, `bearer`, `basic`, `api-key`, `oauth2-client-credentials` |
| No reusable node config system | Node Config Presets — full CRUD backend + frontend `/settings/presets` |
| No Role-Based Access Control (RBAC) | Three roles (Viewer/Editor/Admin) enforced via JWT claims and `[Authorize(Policy=...)]` on all controller actions |
| No Workflow Trigger Types | `TriggerType` enum (Manual/Webhook/Cron); `POST /api/webhooks/{id}`; `CronSchedulerService` using Cronos |
| Workflow Execution Queue is In-Memory | `PostgresExecutionQueue` backed by DB table; SELECT FOR UPDATE SKIP LOCKED; falls back to stub |
| No Execution Retry / Backoff | `RetryPolicy` value object on `Workflow`; engine retries with exponential backoff |
| No Real-Time Execution Log Streaming | SignalR hub `ExecutionHub`; `IExecutionNotifier` at node lifecycle points; `useExecutionStream` hook |
| Tenant Onboarding Flow Missing | `/onboarding` 3-step UI; invite flow; `TenantInvite` entity; `/invite/[token]` page |
| No public landing page | `/` landing page with hero, features, CTA |
| No site footer | `PublicFooter` component on all public pages |
| No cookie consent banner | `CookieBanner` with `localStorage` persistence |
| No Terms / Privacy pages | `/terms` and `/privacy` static pages |
| No Feedback page | `/feedback` page with form (backend stub) |
| No public documentation page | `/docs` section rendering existing markdown files |
| Expired JWT token not handled | `isTokenExpired()` in `auth.ts`; `apiFetch` redirects on 401 or pre-flight expiry |
| No Gmail integration node | `integrations.gmail.read` node — OAuth2 refresh token flow, structured email output |
| No ForEach / loop node | `logic.foreach` fan-out node — expands JSON array into `item_0..N` + `count` + `firstItem` |
| Gmail credentials stored in workflow config | Gmail OAuth2 credential vault: `GmailCredential` entity, `IGmailCredentialRepository`, `/api/gmail/auth/start` → callback → DB store |
| Credential Name was a plain text field | Dropdown in designer populated from `/api/gmail/credentials` + "Connect Gmail" link |
| AI node model field was plain text | `OptionsSource: "llm-models"` on all AI node descriptors; `/api/nodes/models` endpoint; dropdown in designer |
| OpenAI API key only configurable via env var | Settings page: API key (masked), default model, test connection; `PlatformSetting` entity; hot-reload via `OpenAIApiKeyHolder` |
| Default AI provider hardcoded | Settings → AI Providers: "Set as default provider" button per panel; `LLMProviderRouter` reads from DB at call time; no restart needed |
| AI builder available even without API key | `GET /api/settings/ai-status` endpoint; panels check on open and show amber warning + disabled input when unconfigured |
| Form field types missing `file` in AI prompt | Added `file` to `FormGenerationService` system prompt; AI builder now preserves file fields |
| Playground forms stored with PascalCase field keys | `ToResponse` normalizes `FieldsJson` to camelCase on every read; playground seed uses `CamelCase` serializer options |
| `system.data-checkpoint` approvals showed Approve/Reject buttons | Approval detail page detects `_correlationToken` payload and shows resume URL + curl instead |
| Designer nodes had no icons | `CustomNode` renderer with `NODE_ICON_MAP` (20+ icon keys); palette items show icon badge |
| AI panel didn't show which provider/model was active | `ActiveProviderBadge` in both AI panels; per-message footer shows provider, model, token count |
| AI chat history not persisted | `AiChatSession` + `AiChatMessage` entities; `EfAiChatRepository`; `WorkflowGenerationService` + `FormGenerationService` log fire-and-forget; `GET /api/ai/sessions` + messages + usage-summary; `/settings/ai-history` page |
| Nodes appeared disconnected in designer | Edges without `id` field (playground seed) now get `edge-{source}-{target}` auto-generated on load |
| Node config lost on designer restore | `WorkflowDesigner` now reads config from `n.config` (definition format) OR `n.data.config` (RF format) |
| No home link from login/signup/app sidebar | Logo links to `/` on all three |
| Docs code blocks had dark background | Changed to `bg-slate-50 / text-slate-800` light theme |
| Workflow execution history was global only | `/workflows/[id]/executions` page with status filter + auto-refresh |

---

## 🔴 Critical / High Priority

### 1. Plain-text secrets in workflow config JSON
- **Status:** ✅ Resolved — Secret vault infrastructure + UI (`/settings/secrets`). `isSensitive` flag on node config fields: sensitive inputs render as masked password fields with a `{{secret:name}}` prompt and link to the vault in the designer drawer.

### 2. ForEach is fan-out only — no true per-item subgraph loop
- **Problem:** `logic.foreach` outputs `item_0..item_N` which only works for a fixed number of items wired at design time. Can't loop over 50 emails without 50 wired branches.
- **Proposed fix:** Engine-level loop support — ForEach drives a subgraph in a repeat-until pattern; downstream nodes execute once per item.

---

## 🟡 Important Gaps

### 3. Workflow versioning UI
- **Status:** ✅ Resolved — Version History panel in designer toolbar. Load (preview), Activate (set active). `GET /api/workflows/{id}/versions` + `GET /api/workflows/{id}/versions/{versionId}`.

### 4. Execution input UI
- **Status:** ✅ Resolved — `RunWorkflowModal` parses the active version definition, discovers input keys from `system.start` outgoing edges, and renders a dynamic input form before execution.

### 5. Feedback endpoint not wired
- **Status:** ✅ Resolved — `POST /api/feedback` stores to DB (`Feedback` entity), frontend form wired.

### 6. Settings page — additional LLM providers
- **Status:** ✅ Resolved — Anthropic, Azure OpenAI, Ollama added with Settings cards, test endpoints, and unit tests.

### 7. Gmail credential connect flow requires knowing client credentials
- **Status:** ✅ Resolved — Connect flow now uses Settings-stored clientId/clientSecret. Returns helpful error if not configured in Settings.

---

## 🟢 Nice to Have

| Item | Notes |
|---|---|
| Workflow search / filter on list | Backend search param exists; UI only has basic text filter |
| Node execution output viewer | Show raw input/output JSON per node in execution timeline |
| Dark mode | Tailwind dark: classes not yet wired |
| Webhook node — inbound signature verification | `WebhookOutNode` exists; inbound webhook validation (HMAC) not enforced |
| Multi-tenant admin panel | No super-admin view across tenants |
| Export/import workflow definitions | Download/upload workflow JSON |
| Sensitive config field enforcement | Node config fields marked `isSensitive` should auto-mask values in drawer and suggest `{{secret:...}}` instead of raw paste | ✅ Done |
| Ollama model list dynamic | `llm.ollama.models` setting not surfaced in UI; models are hardcoded in provider |
| ForEach loop body branching | Loop body is linear only; no conditional branching inside a loop iteration |
| Workflow run history per workflow | ✅ Done — `/workflows/[id]/executions` page with status filter, per-run view/cancel |
| **Parallel fan-out** | ? Done � engine now walks all outgoing edges; all targets execute sequentially and outputs are merged |
| **Form field regex validation** | ? Done � `validationRegex` + `validationMessage` on FormFieldDefinition; client + server validation |
| **External trigger / webhook wait nodes** | ✅ Done — see plan below (all three items shipped) |
| Worker doesn't reload form nodes when a new form is created after startup | ✅ Done — `WorkerFormNodeRegistrar` now polls the DB every 30 s; new/updated/deleted forms are reflected without a restart. |

---

## 🟢 Working (Verified)

| Feature | Status |
|---|---|
| User login (JWT) + auto-redirect on expiry | ✅ |
| Workflow create / list / save / activate | ✅ |
| Node registry + catalog (23 nodes) | ✅ |
| Designer drag, save, load, delete | ✅ |
| Execution engine (node graph walking) | ✅ |
| Execution enqueue + PostgreSQL worker queue | ✅ |
| Approval inbox + detail page | ✅ |
| HTTP node — all 5 auth types | ✅ |
| Node Config Presets (backend + frontend) | ✅ |
| PostgreSQL + EF Core + migrations | ✅ |
| SignalR real-time execution streaming | ✅ |
| RBAC (Viewer / Editor / Admin) | ✅ |
| Tenant onboarding + invite flow | ✅ |
| Gmail OAuth2 credential vault | ✅ |
| Credential Name dropdown in designer | ✅ |
| LLM model dropdown on all AI nodes | ✅ |
| OpenAI API key via Settings page (hot-reload) | ✅ |
| ForEachNode (fan-out) + GmailReadNode | ✅ |
| ForEach loop mode + loop body node execution recording | ✅ |
| ForEach `inheritOutputs` flag for cross-body-node output sharing | ✅ |
| Undo/Redo in designer (50-step history, Ctrl+Z/Y) | ✅ |
| Version History panel in designer (Load + Activate) | ✅ |
| `ai.extract` format presets (financial/invoice/contact) + prompt injection protection | ✅ |
| Worker reads OpenAI key from DB per-tenant (not env-only) | ✅ |
| Feedback endpoint wired to DB (`POST /api/feedback`) | ✅ |
| Anthropic, Azure OpenAI, Ollama LLM providers + Settings cards | ✅ |
| Gmail credential connect flow uses Settings-stored credentials | ✅ |
| Secrets Vault UI (`/settings/secrets`) — list, add, rotate, delete | ✅ |
| `isSensitive` node config fields — masked drawer inputs + `{{secret:name}}` suggestion | ✅ |
| AI Workflow Assistant (`POST /api/workflows/ai-assist` + `AiAssistPanel` in designer) | ✅ |
| Custom Form Nodes — form builder UI, `form.<slug>` dynamic node, fill page, resume integration | ✅ |
| AI History page (`/settings/ai-history`) — sessions, messages, token usage | ✅ |
| AI provider/model badge in AI panels + per-message token footer | ✅ |
| Configurable default AI provider (Settings → AI Providers → Set as default) | ✅ |
| `system.data-checkpoint` node — field validation, type coercion, retry-on-failure | ✅ |
| Data-checkpoint approval page shows resume URL + curl (not Approve/Reject) | ✅ |
| Designer node icons (CustomNode + NODE_ICON_MAP) | ✅ |
| Workflow execution history per-workflow (`/workflows/[id]/executions`) | ✅ |
| Backend unit tests | ✅ **392 / 392** |
| Frontend unit tests | ✅ **63 / 63** |
| XML / JSDoc docs on all public APIs | ✅ |

---

## 🟡 Plan: External Trigger / Webhook Wait Nodes

Three related capabilities, best implemented together:

### A. External Trigger — start a workflow from outside

A workflow with `TriggerType = Webhook` already supports `POST /api/webhooks/{id}` to start it. This just needs surfacing better in the UI and documentation. **Mostly done.**

### B. `integration.wait-for-webhook` — pause and wait for external data

A new node type that pauses the workflow and waits for a specific external event before proceeding. Use case: "start the approval process, then wait for the external ERP system to confirm before saving."

**How it works:**
1. Node executes, generates a unique `correlationToken` (UUID), returns `WaitingForApproval`
2. Workflow pauses. Token is stored in `NodeExecution.OutputJson`.
3. External system POSTs to `POST /api/webhooks/resume/{correlationToken}` with any payload it wants
4. Engine resumes; the payload becomes node outputs available to downstream nodes

**Config:** `timeoutSeconds` (optional, default none), `expectedFields` (optional list of field names to document what the external system should send)

**Outputs:** whatever fields the external system POSTs + `_resumedAt`, `_correlationToken`

### C. `integration.external-gate` — let external system approve/reject

Similar to `human.approval` but driven by an API call instead of a UI click.

**How it works:**
1. Node generates a token, pauses
2. External system calls `POST /api/webhooks/gate/{token}` with `{ "approved": true, "reason": "...", "data": {...} }`
3. If `approved=true`: engine follows the approved edge; `data` fields flow downstream
4. If `approved=false`: engine follows the rejected edge (or stops if none)

### Implementation plan

| Component | Work |
|-----------|------|
| `WaitForWebhookNode` | New node: generates token, returns `WaitingForApproval` |
| `ExternalGateNode` | New node: generates token, returns `WaitingForApproval` with approve/reject outputs |
| `CorrelationToken` entity | `Id`, `ExecutionId`, `NodeExecutionId`, `Token` (unique), `Kind` (wait/gate), `CreatedAt`, `ExpiresAt?` |
| `POST /api/webhooks/resume/{token}` | Public endpoint; validates token, resumes execution with body as outputs |
| `POST /api/webhooks/gate/{token}` | Public endpoint; accepts `approved`, `reason`, `data`; resumes on approved edge |
| Designer | New `integrations` category nodes visible in palette |
| Execution timeline | Show token + copyable resume URL when node is paused |
| Tests | Token generation, resume flow, timeout handling |

_Update this file as items are resolved._


---

## Additional Workflow Trigger Methods

The following trigger mechanisms have been identified and added to the backlog for future implementation. Cron scheduling and Re-run are already shipped.

### API Key Trigger
- POST /api/workflows/{id}/run?apiKey=*** � no JWT required, for external systems
- Requires: WorkflowApiKey entity (tenantId, workflowId, keyHash, createdAt, lastUsedAt, label)
- API: GET/POST/DELETE /api/workflows/{id}/api-keys
- Engine: same flow as manual execute, triggeredBy = null

### Call Workflow Node
- A new node system.call-workflow that starts a child workflow inline
- Optional: wait for child completion and map child outputs to parent inputs
- Prevents circular references at validation time

### Re-run with Modified Inputs
- Fork an existing execution: POST /api/executions/{id}/rerun with optional { input: {...} } body override
- Frontend: "Re-run with changes" button opens RunWorkflowModal pre-filled with original inputs

### Email Trigger
- Monitored mailbox (IMAP polling or webhook from SendGrid/Mailgun) starts a workflow
- Email metadata (from, subject, body, attachments) available as node inputs
- Requires: email integration credential + trigger type Email

### File Drop Trigger
- Watched folder or S3/GCS/Azure Blob prefix � new file starts a workflow
- File content / metadata available as node inputs
- Requires: storage integration + trigger type FileDrop

### Slack Command Trigger
- Slash command or @mention in Slack starts a workflow
- Payload (user, channel, text) available as inputs
- Requires: Slack app integration

### Bulk / CSV Run
- Upload a CSV from the UI � one execution per row, each row's columns as inputs
- Frontend: "Run from CSV" button on the workflow page
- Backend: POST /api/workflows/{id}/bulk-run with multipart CSV upload
