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
│   │       ├── page.tsx                  # settings hub (cards)
│   │       ├── providers/page.tsx        # AI providers (dropdown + panel)
│   │       ├── integrations/page.tsx     # external integrations (Gmail etc.)
│   │       ├── secrets/page.tsx          # secret vault
│   │       └── presets/page.tsx          # node config presets
│   ├── forms/[id]/fill/page.tsx          # public form fill (no auth)
│   └── invite/[token]/page.tsx
├── components/
│   ├── ui/                               # Badge, Button, Card, Input, PageHeader, EmptyState
│   ├── designer/                         # WorkflowDesigner, NodePalette, NodeConfigDrawer, VersionHistoryPanel, AiAssistPanel
│   └── RunWorkflowModal.tsx
├── lib/
│   ├── api.ts                            # typed API client (all apiFetch calls)
│   ├── auth.ts
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
      { href: '/settings/providers',    label: 'AI Providers',  icon: Cpu },
      { href: '/settings/integrations', label: 'Integrations',  icon: Plug },
      { href: '/settings/secrets',      label: 'Secrets',       icon: KeyRound },
    ],
  },
];
```

- Top-level items highlight in **indigo** when active.
- Sub-items show indented with a left border accent, highlighted in **violet** when active, and are only visible when their parent (or a sibling child) is active.
- To add sub-items to any nav entry, add a `children` array — no other changes needed.

---

## 4. Screens

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
Cards linking to sub-sections:
- **AI Providers** (`/settings/providers`) — dropdown selector + config panel per provider (OpenAI, Anthropic, Azure OpenAI, Ollama). Adding a new provider = one entry in the `PROVIDERS` array + one panel component.
- **Integrations** (`/settings/integrations`) — external service credentials (Gmail OAuth2). Same dropdown pattern.
- **Secrets** (`/settings/secrets`) — encrypted named vault; reference as `{{secret:name}}` in any node config.

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

---

## 5. `statusLabel()` and `statusVariant()`

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
