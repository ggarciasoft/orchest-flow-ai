# Frontend (Web App)

The web app is a Next.js 15+ application. It hosts the workflow designer, dashboard, approval inbox, execution timeline, and admin screens.

---

## 1. Stack

- **Next.js 15+** (App Router)
- **React 19+** with TypeScript (strict)
- **Tailwind CSS** for styling
- **React Flow** for the workflow canvas
- **TanStack Query** for server state

---

## 2. Top-Level Structure

```
apps/web/
├── app/
│   ├── (auth)/login/
│   ├── (app)/
│   │   ├── layout.tsx                    # sidebar nav, auth guard
│   │   ├── dashboard/
│   │   ├── workflows/
│   │   │   ├── page.tsx                  # list + Duplicate button
│   │   │   ├── new/page.tsx
│   │   │   └── [id]/designer/page.tsx    # workflow designer
│   │   ├── executions/
│   │   │   ├── page.tsx                  # list
│   │   │   └── [id]/page.tsx             # timeline
│   │   ├── approvals/
│   │   │   ├── page.tsx                  # inbox
│   │   │   └── [id]/page.tsx             # detail (form or approve/reject)
│   │   ├── forms/
│   │   │   ├── page.tsx                  # forms list
│   │   │   ├── [id]/page.tsx             # form builder
│   │   │   └── _components/FormRenderer.tsx
│   │   ├── documents/page.tsx
│   │   └── settings/
│   │       ├── page.tsx                  # settings hub (role-filtered cards)
│   │       ├── team/page.tsx             # team members + invitations (admin)
│   │       ├── tenant/page.tsx           # workspace config (admin)
│   │       ├── providers/page.tsx        # AI providers (admin)
│   │       ├── integrations/page.tsx     # external integrations (admin)
│   │       ├── secrets/page.tsx          # secret vault (admin)
│   │       ├── presets/page.tsx          # node config presets (editor+)
│   │       ├── config/page.tsx           # workflow key-value config
│   │       └── ai-history/page.tsx       # AI chat history
│   ├── forms/[id]/fill/page.tsx          # public form fill (no auth)
│   └── invite/[token]/page.tsx           # invite accept (no auth)
├── components/
│   ├── ui/                               # Badge, Button, Card, Input, PageHeader, EmptyState
│   ├── designer/                         # WorkflowDesigner, NodePalette, NodeConfigDrawer, VersionHistoryPanel, AiAssistPanel
│   ├── AdminPageGuard.tsx                # redirects non-admins away from admin pages
│   └── RunWorkflowModal.tsx
├── contexts/
│   └── AuthContext.tsx                   # AuthProvider + useAuth() — role, canEdit, isAdmin, isApprover
├── lib/
│   ├── api.ts                            # typed API client (all apiFetch calls)
│   ├── auth.ts                           # JWT helpers: getToken, setToken, getRoleFromToken, getTenantId
│   └── utils.ts
└── src/__tests__/
```

---

## 3. Sidebar Navigation

The sidebar nav in `(app)/layout.tsx` supports nested sub-items via a `children` array on any nav entry:

```ts
const nav: NavItem[] = [
  { href: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/workflows',  label: 'Workflows',  icon: GitBranch },
  { href: '/forms',      label: 'Forms',      icon: ClipboardList },
  { href: '/executions', label: 'Executions', icon: Play },
  { href: '/approvals',  label: 'Approvals',  icon: CheckSquare },
  { href: '/documents',  label: 'Documents',  icon: FileText },
  {
    href: '/settings',
    label: 'Settings',
    icon: Settings,
    children: [
      // Admin-only (minRole: 'admin')
      { href: '/settings/tenant',       label: 'Tenant',        icon: Building2  },
      { href: '/settings/team',         label: 'Team',          icon: Users      },
      { href: '/settings/providers',    label: 'AI Providers',  icon: Cpu        },
      { href: '/settings/integrations', label: 'Integrations',  icon: Plug       },
      { href: '/settings/secrets',      label: 'Secrets',       icon: KeyRound   },
      // Editor+ (minRole: 'editor')
      { href: '/settings/presets',      label: 'Presets',       icon: BookOpen   },
      // All roles
      { href: '/settings/config',       label: 'Configuration', icon: SlidersHorizontal },
      { href: '/settings/ai-history',   label: 'AI History',    icon: MessageSquare },
    ],
  },
];
```

- Top-level items highlight in **indigo** when active.
- Sub-items are **filtered by role** (via `AuthContext`) — admins see all settings; editors see a subset; viewers see only Configuration and AI History.
- Sub-items show indented with a left border accent, highlighted in **violet** when active.

### Role Badge

The sidebar footer displays the authenticated user's display name alongside a role badge:

| Role | Color |
|------|-------|
| Admin | Violet |
| Editor | Indigo |
| Approver | Amber |
| Viewer | Slate |

---

## 4. Screens

### Home Page (`/`)
Public marketing landing page. Not authenticated.

- **Nav** — logo, Docs link, Sign in, Get started (→ /onboarding)
- **Hero** — headline, subtitle, CTA buttons (Start for free → /onboarding, Sign in → /login)
- **Feature grid** (6 cards, 3-column on large screens):
  - Visual Workflow Designer — canvas with 20+ node types, icons, AI assistant
  - Multi-Provider AI — OpenAI / Anthropic / Azure / Ollama, switchable without restart
  - Custom Forms & Approvals — AI-assisted form builder, pause-for-human-input
  - External Data Intake — `system.data-checkpoint` node, field validation, retry on failure
  - Enterprise Security — JWT, RBAC, tenant isolation, encrypted secrets
  - Real-Time Monitoring — execution timeline, AI chat history, token usage
- **CTA section** — "Create your workspace" → /onboarding
- **Footer** — `PublicFooter` component

### Dashboard
Total workflows, recent executions, pending approvals, failed executions.

### Workflows List
- Name, active version tag, last updated, Run button, **Duplicate** button, Designer link.
- **Search** — debounced name search (350 ms); resets to page 1 on change.
- **Pagination** — prev/next controls; 20 per page; total count from API.
- **Duplicate** calls `POST /api/workflows/{id}/clone` → opens the copy in the designer immediately.
- Duplicate is disabled when the source has no active version.

### Workflow Designer

Three regions:

1. **Left sidebar** — node palette with categories. Drag to canvas.
2. **Main canvas** — React Flow. Click node to open config drawer.
3. **Right drawer** — config rendered dynamically from `/api/nodes/catalog`.

Top bar:
- **Workflow name** — click the pencil icon (✏️) next to the name to edit name + description inline. Enter to save, Escape to cancel. Calls `PUT /api/workflows/{id}`.
- **Undo / Redo** — 50-step history. `Ctrl+Z` / `Ctrl+Y`.
- **Save** — creates new version + activates via `POST /api/workflows/{id}/versions`.
- **History** — Version History panel. Load (preview) or Activate any saved version.
- **Run** — input dialog → `POST /api/workflows/{id}/execute`.

**AI Assistant:**
- **ActiveProviderBadge** ? shown below panel subtitle; reads `/api/settings/ai-status` on mount and displays provider + model badges
- **Per-message usage footer** ? each assistant response shows provider, model, and token count
- **Disabled state** ? if `isDefaultConfigured: false`, shows amber warning banner with link to Settings ? AI Providers; input + send button disabled

**Custom Node Renderer:**
Nodes on the canvas use a `CustomNode` React Flow component:
- Category-colored background (same `CATEGORY_COLORS` map as before)
- Lucide icon mapped from `descriptor.iconKey` via `NODE_ICON_MAP`
- First-letter fallback when no icon matches
- Connection handles on top (target) and bottom (source)
- Node type: `'custom'` (registered via `nodeTypes` prop)

**Node Palette Icons:**
Each palette item shows:
- Small colored icon badge (category color + icon)
- First-letter fallback when iconKey has no match

**Edge ID Generation:**
> **Edge IDs** ? edges without an `id` field (e.g. from the playground seed) automatically get `edge-{source}-{target}` assigned on load so React Flow renders them correctly.

### Execution Timeline
- Per-node status, timestamps, input/output JSON, retry count.
- Status badges use `statusLabel()` for human-readable text (e.g. `WaitingForApproval` → `"Waiting for Approval"`).
- **Cancel Execution** button appears in the header when status is `Queued`, `Running`, or `Paused`. Opens `CancelExecutionModal` — an amber warning dialog showing workflow name/version and execution ID. Calls `POST /api/executions/{id}/cancel`.
- **Waiting for Approval banner** — when the execution is `Paused`, the detail page fetches the pending approval via `GET /api/approvals/by-execution/{id}`. If one exists, an amber clickable banner links directly to `/approvals/{id}`. The `WaitingForApproval` node row in the timeline also shows an inline "Review" link.

### Approval Inbox / Detail

The approval detail page (`/approvals/[id]`) behaves differently depending on the node type that triggered the pause:

#### Form-node approvals (`_formId` present in payload)
- Renders the form fields using `FormRenderer` so the user can fill in the data.
- Required-field validation runs client-side before submit.
- Submit calls `POST /api/forms/{id}/submit` → resumes the workflow execution.
- Shows a **"Form submission"** pill badge to distinguish from human-approval nodes.

#### Human-approval nodes (no `_formId`)
- Shows context payload fields (non-`_` keys).
- **Approve** / **Reject** with optional comment textarea.

All badges use `statusLabel()` — `WaitingForApproval` displays as amber "Waiting for Approval", `Succeeded` as green, `Failed` as red, etc.

### Settings Hub (`/settings`)
Cards linking to sub-sections. Cards are **filtered by role** — admins see all; editors see a subset; viewers see only Configuration and AI History.

**Team** (`/settings/team`) _(Admin only)_:
- **Invite form** — email + role picker (Admin/Editor/Approver/Viewer) + "Send invite" button. Sends an invitation email; shows "✓ email sent" confirmation. Errors (duplicate member, duplicate invite, invalid role) shown inline.
- **Members list** — all users in the tenant. Click a role badge to edit it inline (dropdown + Save/Cancel). Trash icon to remove a member. Both actions are disabled for the caller's own account.
- **Pending invites list** — invites not yet accepted. Shows email, role badge, expiry date. X button to revoke.

**AI Providers** (`/settings/providers`) _(Admin only)_:
- Dropdown selector + config panel per provider (OpenAI, Anthropic, Azure OpenAI, Ollama)
- **Active provider banner** — reads `llm.defaultProvider` + `llm.defaultModel` from settings
- **"Set as default provider" button** — calls `PUT /api/settings`; takes effect immediately (no restart)
- Adding a new provider = one entry in the `PROVIDERS` array + one panel component

- **Tenant** (`/settings/tenant`) _(Admin only)_ — workspace name, logo URL, execution limits, timezone, guest form fill flag.
- **Integrations** (`/settings/integrations`) _(Admin only)_ — external service credentials (Gmail OAuth2). Same dropdown pattern.
- **Secrets** (`/settings/secrets`) _(Admin only)_ — encrypted named vault; reference as `{{secret:name}}` in any node config.
- **Node Presets** (`/settings/presets`) _(Editor+)_ — reusable named node config sets.
- **Configuration** (`/settings/config`) _(All roles)_ — persistent workflow key-value store.

All admin-only settings pages are wrapped with `<AdminPageGuard>` — non-admins are redirected to `/settings`.

### AI History (`/settings/ai-history`)
Read-only view of all AI chat sessions for the tenant.

- **Usage summary cards** ? total sessions, total tokens, breakdown by surface (Workflow Designer / Form Generator)
- **Filter** ? dropdown to filter by surface
- **Session list** ? table with Surface, Date, session count columns; expand any session to view its message thread
- **Message thread** ? chat bubbles: user (right/slate), assistant (left/indigo, with model+token footer), tool (left/amber, with collapsible input/output JSON)
- Calls `GET /api/ai/sessions`, `GET /api/ai/sessions/{id}/messages`, `GET /api/ai/usage-summary`

### Executions List (`/executions`)
- **Status filter pills** — All / Running / Queued / Paused / Completed / Failed / Cancelled. Resets to page 1.
- **Search** — debounced (350 ms) match against Correlation ID.
- **Pagination** — 20 per page with accurate total.
- **Cancel** icon on active rows (Queued/Running/Paused) — opens `CancelExecutionModal`.
- Auto-refreshes every 10 s.

### Forms Builder
- `/forms` — list layout (not card grid); **search** by name or description; **pagination** (20 per page).
- Copy node type (`form.<slug>`) to clipboard inline.
- `/forms/[id]` — builder with Name, Slug (auto-generated from name, manually overridable), Description, field list (reorder, add/edit/delete), Preview modal, **AI assistant panel** (generate/modify fields via LLM prompt).
- `slug` is **required** and determines the node type. Must be unique per tenant.
- **Field types**: `text`, `number`, `select`, `date`, `email`, `boolean`, **`file`**.
  - `file` fields render a styled upload zone on the fill page. On file selection the file is uploaded via `POST /api/documents/upload`; the field value becomes `{ id, filename, mimeType }`. Optional **Accepted file types** config (e.g. `.pdf,.png`) restricts choosable files.
- **Version History panel** (existing forms only) — collapsible panel in the left column showing all saved versions. Each entry shows version number, date, field count, and active badge.
  - **Activate** button on any past version rolls back the form's active definition. The builder immediately reflects the rolled-back fields. The engine picks up the change within 30 s (worker polling).
  - Saving the form always creates a new version — previous versions are never overwritten.
- Every save (create or update) automatically creates a `FormVersion` snapshot and activates it.

### Form Fill Page (`/forms/[id]/fill`)
Public (no auth). Renders form + submits values to resume the paused execution.

### Invite Accept Page (`/invite/[tenantId]?token=...`)
Public (no auth). Allows an invitee to set a password and join the workspace.

1. On mount: calls `GET /api/tenants/{id}/invite/preview?token=` — shows workspace name, invited email, assigned role in a highlighted banner.
2. If preview returns 404/error: shows "Invite not found" error state — no password form.
3. On password submit: calls `POST /api/tenants/{id}/invite/accept` → receives JWT → stores token via `setToken()` → redirects to `/workflows`.
4. "Already have an account?" link to `/login`.

### Onboarding (`/onboarding`)
Multi-step flow for new users:

1. **Step 1** — Name your workspace (`POST /api/tenants`)
2. **Step 2** — Invite team members (`POST /api/tenants/{id}/invite`); shows "✓ email sent" confirmations per invite
3. **Step 3** — Done; link to dashboard

---

## 5. Authentication & RBAC

### `AuthContext` (`src/contexts/AuthContext.tsx`)

React Context providing the authenticated user's state, derived from the JWT in `localStorage`. Hydrated client-side on mount.

```ts
interface AuthState {
  role: 'Admin' | 'Editor' | 'Approver' | 'Viewer' | null;
  displayName: string;
  email: string;
  canEdit: boolean;    // true for Editor and Admin
  isAdmin: boolean;    // true for Admin only
  isApprover: boolean; // true for Approver (and Admin)
}
```

Usage in any component:

```tsx
const { isAdmin, canEdit, isApprover, role, displayName } = useAuth();
```

`AuthProvider` is registered in `components/providers.tsx` and wraps the entire app.

### Role-Gated UI

| Guard | How |
|-------|-----|
| Admin-only pages | Wrapped with `<AdminPageGuard>` — redirects to `/settings` if not admin |
| Sidebar nav filtering | `SETTINGS_CHILDREN` filtered in `layout.tsx` via `useMemo` |
| Mutation buttons | Conditional render based on `canEdit` / `isAdmin` / `isApprover` |
| Settings hub cards | Conditional render based on `isAdmin` / `canEdit` |

### `AdminPageGuard` (`src/components/AdminPageGuard.tsx`)

Wraps a page's content; redirects to `/settings` if `!isAdmin`. Shows a brief "Admin access required" message during the redirect.

Applied to: `/settings/tenant`, `/settings/team`, `/settings/providers`, `/settings/integrations`, `/settings/secrets`.

---

## 6. `statusLabel()` and `statusVariant()`

Both exported from `@/components/ui`:

```ts
statusVariant(status: string): BadgeVariant
// 'WaitingForApproval' → 'warning' (amber)
// 'Completed' | 'Approved' | 'Succeeded' → 'success'
// 'Failed' | 'Rejected' → 'danger'
// 'Running' | 'Processing' → 'info'
// 'Pending' → 'warning'

statusLabel(status: string): string
// 'WaitingForApproval' → 'Waiting for Approval'
// 'InProgress' → 'In Progress'
// Any other CamelCase → inserts spaces before capitals
```

Use `statusLabel(status)` as the badge text everywhere statuses are displayed.

---

## 6. `apiFetch` Error Handling

`apiFetch` in `lib/api.ts` reads error responses in priority order:

1. JSON with `errors` object (ASP.NET validation) → formats as `"Field: message"`
2. JSON with `detail` or `title` → uses that string
3. Plain-text body → uses raw text (e.g. `"Slug is required."`)
4. Fallback → `"HTTP {status}"`

This means backend `BadRequest("...")` plain-text responses reach the user correctly.

---

## 7. Data Layer

All server state flows through TanStack Query keyed on `[entity, params]`. The API client in `lib/api.ts` is the single source of truth for all HTTP calls — no bare `fetch()` calls anywhere.

---

## 8. Front-End Rules

1. **No hardcoded node forms** — render config forms dynamically from `/api/nodes/catalog`.
2. **No bare `fetch` calls** — use `apiFetch` from `api.ts`.
3. **No `any` in TypeScript**.
4. `apiFetch` uses `res.text()` then attempts JSON parse — supports empty body responses; test mocks need `text()` too.
5. Show validation errors before save; required field errors surface immediately.
6. Execution and timeline views are **read-only**.

---

## 9. Testing

- **Unit / integration:** React Testing Library (`npx jest --watchAll=false`).
- Mocked API calls; `QueryClientProvider` wraps all component tests that use TanStack Query hooks.
- Designer tests mock `@xyflow/react`, `lucide-react`, and `@/lib/api`.

---

### Workflow Playground (/playground)
Demo/testing page for the full form-node execution flow.

- **Start** button calls POST /api/playground/seed (idempotent) to set up the sample workflow, then triggers execution via POST /api/workflows/{id}/execute.
- **Forms created** ? three form nodes are seeded: `pg-personal-info` (Full Name, Email, Date of Birth), `pg-employment` (Company, Job Title, Start Date), `pg-preferences` (Newsletter, Timezone, Notes). Forms are idempotent ? re-seeding reuses existing forms if their slug already exists.
- **Step indicator** � three pills (Personal Info / Employment / Preferences) highlight current and completed steps.
- **Polling** � GET /api/executions/{id} every 2 s detects status transitions; GET /api/approvals/by-execution/{id} fetches the pending form when the execution is Paused.
- **Inline FormRenderer** � renders the paused form step; submits via POST /api/forms/{id}/submit and resumes polling for the next step.
- **Completion screen** � shows all collected data from every step in a summary view once the workflow reaches system.end.
- **Run Again** resets all state and allows restarting from scratch.
### External Data Playground (/playground/external)
Demo/testing page for the `system.data-checkpoint` node - purely API-driven, no form rendering.

- **Setup screen** ? shown on first load; lets the user configure the two database nodes (Customer DB + Order DB) with connection string and SQL statement. Each section includes a copyable `CREATE TABLE` SQL snippet. "Skip DB setup" skips DB config entirely; "Save & Continue" validates and persists config for the seed call.
- **Start** button calls POST /api/playground/seed-external with optional `{ customer: { connectionString, statement }, order: { connectionString, statement } }` body to seed a workflow with 2 data checkpoints and 2 DB-execute nodes, then starts execution.
- **Checkpoint indicator** - two pills (Customer / Order) track which checkpoint is active/completed.
- **Pause panel** - when the workflow pauses at a checkpoint, shows the full resume URL with a copy button.
- **Simulate External System panel** - JSON textarea pre-filled with example data + Send button that POSTs to `/api/webhooks/resume/{token}` via `api.webhooks.resume()`.
- **Field validation** ? checkpoint nodes are seeded with `fields` config (`name`+`email` required for Customer; `items`+`amount` required for Order, `amount` type=number). If the posted JSON is missing a required field or fails type validation, the execution is marked Failed with a descriptive error shown in the activity log.
- **curl command** - live curl snippet (updates as JSON is edited) with a Copy button for testing from a real terminal.
- **Activity log** - timestamped log of all events (seeding, polling, pauses, resumes, errors).
- **Completion screen** - shows data collected at each checkpoint once the workflow reaches system.end.
- **Run Again** resets all state.
- Uses `apiFetch` exclusively (no bare fetch); no `any` TypeScript.
