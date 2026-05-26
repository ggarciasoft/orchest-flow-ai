# OrchestFlowAI — Known Gaps & Work Backlog

> Created: 2026-05-23  
> Last updated: 2026-05-25

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
| No Workflow Trigger Types | `TriggerType` enum (Manual/Webhook/Cron) added to domain; `POST /api/webhooks/{id}` endpoint; `CronSchedulerService` background service using Cronos |

---

## 🔴 Critical Gaps (Blocking End-to-End Execution)

### 1. ~~Workflow Execution Queue is In-Memory~~ ✅ RESOLVED
- **Problem:** `InMemoryExecutionQueue` uses .NET Channels. Doesn't survive restarts, no horizontal scaling.
- **Resolution:** Implemented `PostgresExecutionQueue` backed by the `ExecutionQueue` table in PostgreSQL. Uses SELECT FOR UPDATE SKIP LOCKED for atomic multi-worker dequeue. Registered as `IPersistentExecutionQueue`. Falls back to `StubExecutionQueue` (in-memory, `ConcurrentQueue`) when no `CONNECTION_STRING` is set.

---

## 🟡 Important Gaps (Partial Functionality)

### 3. ~~No Execution Retry / Backoff~~ ✅ RESOLVED
- **Problem:** If a node fails (e.g. HTTP 503), the whole execution fails. There's no automatic retry.
- **Resolution:** Implemented `RetryPolicy` value object on `Workflow` entity. Engine honors `MaxAttempts` with exponential backoff via `GetDelay(attemptNumber)`. Configure via `RetryMaxAttempts`, `RetryBackoffMs`, `RetryBackoffMultiplier` in create/update workflow requests.

### 4. ~~No Real-Time Execution Log Streaming~~ ✅ RESOLVED
- **Problem:** Execution status only shows on polling refresh. No live node-by-node updates.
- **Resolution:** Implemented SignalR hub (`ExecutionHub` at `/hubs/execution`). Backend engine calls `IExecutionNotifier` at `NodeStarted`, `NodeCompleted`, `NodeFailed`, and `ExecutionCompleted` lifecycle points. Frontend `useExecutionStream` hook connects, joins the execution group, and accumulates events shown in a live panel on the execution detail page. `StubExecutionNotifier` used when no `CONNECTION_STRING` is configured; `SignalRExecutionNotifier` active otherwise.

### 5. No Role-Based Access Control (RBAC)
- **Problem:** All authenticated users have full access to all workflows/executions in the tenant. No per-workflow permissions, no "viewer" vs "editor" vs "admin" distinction.
- **Proposed fix:** Add `Role` field to `User`; guard controller endpoints with role claims.

### ~~6. Tenant Onboarding Flow Missing~~ ✅ RESOLVED
- **Problem:** There's no UI or guided flow for creating a new tenant or inviting team members.
- **Resolution:** Implemented in `feat: tenant onboarding flow and team invite`.
  - `POST /api/tenants` — create tenant (AdminOnly)
  - `GET /api/tenants/{id}` — get tenant info (ViewerOrAbove)
  - `POST /api/tenants/{id}/invite` — invite user by email (AdminOnly)
  - `POST /api/tenants/{id}/invite/accept` — accept invite, creates user account
  - `/onboarding` page — 3-step guided UI
  - `/invite/[tenantId]` page — accept invite and set password
  - `TenantInvite` domain entity with expiry and acceptance logic
  - EF migration `AddTenantInvites`
  - Full unit test coverage (backend + frontend)

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
| Backend unit tests | ✅ **316 / 316** |
| Frontend unit tests | ✅ **62 / 62** |
| XML / JSDoc docs on all public APIs | ✅ |

---

## 🟡 Marketing / Public-Facing

### ~~7. No public landing page~~ ✅ RESOLVED
- **Problem:** The app has no marketing/home page. Visitors who aren't logged in see nothing meaningful.
- **Resolution:** Added `/` landing page (outside the `(app)` route group) with hero section, feature highlights, and CTA to sign up or sign in.

### ~~8. No site footer~~ ✅ RESOLVED
- **Problem:** No footer exists on any page (public or app). Missing legal/nav links.
- **Resolution:** Added `PublicFooter` component with links to Terms, Privacy, and Feedback. Shown on landing page, terms, privacy, and feedback pages.

### ~~9. No cookie consent banner~~ ✅ RESOLVED
- **Problem:** No GDPR/cookie consent popup. Required for EU compliance and any analytics use.
- **Resolution:** Added `CookieBanner` component that appears on first visit, stores consent in `localStorage`, and disappears on accept/decline. Injected in root layout. Links to Privacy Policy.

### ~~10. No Terms of Service / Privacy Policy pages~~ ✅ RESOLVED
- **Problem:** Footer links to Terms and Privacy have no destination pages.
- **Resolution:** Added `/terms` and `/privacy` static pages with appropriate placeholder content.

### ~~11. No Feedback page/link~~ ✅ RESOLVED
- **Problem:** No feedback channel from within the product.
- **Resolution:** Added `/feedback` page with name, email, and message form. Shows success state on submit. Backend wiring via TODO comment for `POST /api/feedback`.

### 12. ~~No public documentation page~~ ✅ RESOLVED
- **Problem:** The project has rich docs in `/docs/*.md` but no user-facing documentation site.
- **Resolution:** Added `/docs` section in the Next.js app rendering existing markdown files as styled pages. Sidebar with categories, `react-markdown` + `remark-gfm` renderer, `@tailwindcss/typography` prose styles.

---

## Suggested Priority Order

1. ~~Workflow trigger types~~ ✅
2. ~~Execution retry / backoff~~ ✅
3. ~~Real-time execution log streaming~~ ✅
4. ~~RBAC~~ ✅
5. ~~Tenant onboarding UI~~ ✅
6. ~~Replace in-memory queue~~ ✅
7. **Public landing page** — first impression for new users
8. **Footer + legal pages** (Terms, Privacy) — compliance baseline
9. **Cookie consent banner** — GDPR compliance
10. **Feedback page** — user engagement

---

_Update this file as items are resolved._

