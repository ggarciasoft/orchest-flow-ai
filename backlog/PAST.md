# OrchestFlowAI — Past Backlog

> Everything that has been built and verified.

| Item | Resolution |
|---|---|
| No real database — in-memory only | PostgreSQL + EF Core; `OrchestFlowAIDbContext`; auto-migration on startup |
| Designer Save not implemented | `POST /api/workflows/{id}/versions` wired to canvas serialization |
| JWT auth not persisted in frontend | Auth guard on `(app)/layout.tsx`; `localStorage` token verified |
| AI nodes require real API key | `LLM_PROVIDER=fake` dev fallback; `.env.example` documented |
| Workflow definition not loaded into designer | Fetches active version on mount, hydrates ReactFlow canvas |
| Approval detail page missing | `/approvals/[id]` page with timeline |
| Swagger not enabled in all envs | Swagger + JWT auth button shown in all environments |
| Docker Compose all-in-one | Split into `docker-compose.yml` (infra) / `docker-compose.app.yml` / `docker-compose.observability.yml` |
| HTTP node — only one auth mode | 5 auth types: `none`, `bearer`, `basic`, `api-key`, `oauth2-client-credentials` |
| No reusable node config system | Node Config Presets — full CRUD backend + frontend `/settings/presets` |
| No RBAC | Three roles (Viewer/Editor/Admin) enforced via JWT claims and `[Authorize(Policy=...)]` |
| No Workflow Trigger Types | `TriggerType` enum (Manual/Webhook/Cron); `POST /api/webhooks/{id}`; `CronSchedulerService` |
| Workflow Execution Queue is In-Memory | `PostgresExecutionQueue` backed by DB table; SELECT FOR UPDATE SKIP LOCKED |
| No Execution Retry / Backoff | `RetryPolicy` value object; engine retries with exponential backoff |
| No Real-Time Execution Log Streaming | SignalR hub `ExecutionHub`; `IExecutionNotifier` at node lifecycle points |
| Tenant Onboarding Flow Missing | `/onboarding` 3-step UI; invite flow; `TenantInvite` entity |
| No public landing page | `/` landing page with hero, features (6 cards), CTA |
| No public documentation page | `/docs` section rendering markdown files with How To guides |
| Expired JWT token not handled | `isTokenExpired()` in `auth.ts`; `apiFetch` redirects on 401 |
| No Gmail integration node | `integrations.gmail.read` node — OAuth2 refresh token flow |
| No ForEach / loop node | `logic.foreach` fan-out node + loop body execution mode |
| Gmail credentials stored in workflow config | Gmail OAuth2 credential vault; `/api/gmail/auth/start` → callback → DB |
| AI node model field was plain text | `OptionsSource: "llm-models"`; `/api/nodes/models` endpoint; dropdown in designer |
| OpenAI API key only via env var | Settings page: key (masked), default model, test connection; hot-reload |
| Plain-text secrets in workflow config | Secret vault (`/settings/secrets`); `isSensitive` flag; `{{secret:name}}` |
| Default AI provider hardcoded | Settings → AI Providers: "Set as default provider"; router reads DB at call time |
| AI builder crash on unconfigured provider | `GET /api/settings/ai-status`; panels disabled with amber banner when unconfigured |
| Form field types missing `file` in AI prompt | Added `file` to `FormGenerationService` system prompt |
| Playground forms stored with PascalCase field keys | `ToResponse` normalizes `FieldsJson` to camelCase on every read |
| `system.data-checkpoint` approvals showed Approve/Reject | Approval detail page shows resume URL + curl for data-checkpoint tokens |
| Designer nodes had no icons | `CustomNode` renderer with `NODE_ICON_MAP` (20+ icon keys); palette icon badges |
| AI panel didn't show provider/model | `ActiveProviderBadge` in both AI panels; per-message token footer |
| AI chat history not persisted | `AiChatSession` + `AiChatMessage` entities; `/settings/ai-history` page |
| Nodes appeared disconnected in designer | Edges without `id` get `edge-{source}-{target}` auto-generated on load |
| Node config lost on designer restore | Designer reads config from `n.config` OR `n.data.config` |
| No home link from login/signup/app sidebar | Logo links to `/` on all three |
| Docs code blocks had dark background | Changed to `bg-slate-50 / text-slate-800` light theme |
| Workflow execution history was global only | `/workflows/[id]/executions` page with status filter + auto-refresh |
| No persistent workflow state store | `WorkflowConfig` entity; `system.read-config` + `system.write-config` nodes; `/settings/config` |
| Gmail Read query was config-only | Added `query` as wired input — wire from Read Config or Set Variable |
| AI builders had no disclaimer | `⚠️ AI can make mistakes` banner in both AI panels |
| No per-workflow How To docs | HOWTO-DESIGNER, HOWTO-FORM-BUILDER, HOWTO-AI-BUILDER, HOWTO-EXTERNAL-DATA, HOWTO-CONFIG |
| ForEach loop body had no retry and swallowed failures | Body nodes now respect `RetryPolicy` (same backoff as main path); failure propagates to execution status (`return` instead of `break`) |
| Stale `ExecutionsControllerTests` / `FormsControllerTests` constructor mismatches | All 476 tests pass; constructor signatures already aligned — backlog note was outdated |
