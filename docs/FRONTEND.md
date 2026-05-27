# Frontend (Web App)

The web app is a Next.js 14+ application. It hosts the workflow designer, dashboard, approval inbox, execution timeline, and admin screens.

---

## 1. Stack

- **Next.js 14+** (App Router)
- **React 18+** with TypeScript (strict)
- **Tailwind CSS** for styling
- **shadcn/ui** for components
- **React Flow** for the workflow canvas
- **TanStack Query** for server state
- **Zod** for client-side validation
- **OpenAPI codegen** (or `orval`) for a typed API client

---

## 2. Top-Level Structure

```
apps/web/
├── app/
│   ├── (auth)/login/
│   ├── (app)/
│   │   ├── dashboard/
│   │   ├── workflows/
│   │   │   ├── page.tsx                  # list
│   │   │   └── [id]/
│   │   │       ├── page.tsx              # detail
│   │   │       └── designer/page.tsx     # workflow designer
│   │   ├── executions/
│   │   │   ├── page.tsx                  # list
│   │   │   └── [id]/page.tsx             # timeline
│   │   ├── approvals/page.tsx
│   │   ├── documents/page.tsx
│   │   ├── nodes/page.tsx                # catalog
│   │   └── settings/page.tsx
│   ├── api/                              # Next.js route handlers if needed (auth bridge)
│   └── layout.tsx
├── components/
│   ├── ui/                               # shadcn/ui primitives
│   ├── designer/                         # canvas, node palette, config drawer
│   ├── RunWorkflowModal.tsx              # modal: fetches active version def, renders dynamic input form, executes
│   ├── timeline/
│   ├── approvals/
│   └── shared/
├── lib/
│   ├── api/                              # generated client
│   ├── hooks/
│   └── utils/
├── styles/
└── public/
```

---

## 3. Screens (MVP)

### Dashboard

- Total workflows, recent executions, pending approvals, failed executions, AI usage summary (last 7 days).
- Empty states explicit (no fake data).

### Workflows List

- Name, active version, status, last execution, created by.
- Actions: open designer, view executions, archive.

### Workflow Designer

The most important screen. Three regions:

1. **Left sidebar** — node palette with categories (AI, Documents, Logic, Human, Integrations, System). Categories collapse. Drag a node onto the canvas to add it.
2. **Main canvas** — React Flow surface with nodes and edges. Selecting a node opens the right drawer.
3. **Right drawer** — node configuration form rendered dynamically from the node descriptor (`/api/nodes/catalog`). Boolean fields render as a `true/false` select. Enum fields with `optionDescriptions` show a formatted preview box below the dropdown when a value is selected.

Top toolbar actions:

- **↩ Undo** / **↪ Redo** — 50-step history stack. Keyboard: `Ctrl+Z` / `Ctrl+Y` (or `Ctrl+Shift+Z`). Tracks node add/delete, edge add/delete, config changes.
- **Save** — serializes canvas and creates a new version via `POST /api/workflows/{id}/versions`, then activates it.
- **History** — toggles the Version History panel (replaces the config drawer while open). Lists all saved versions newest-first with **Load** (preview on canvas without activating) and **Activate** actions.
- **Run** — opens an input dialog → `POST /api/workflows/{id}/execute`.

Keyboard shortcuts:

| Key | Action |
|-----|--------|
| `Del` / `Backspace` | Delete selected node or edge (skipped when typing in inputs) |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` / `Ctrl+Shift+Z` | Redo |

Version History Panel (`VersionHistoryPanel`):

- Lists all versions from `GET /api/workflows/{id}/versions` (newest first)
- ✅ badge on the currently active version
- **Load**: fetches definition from `GET /api/workflows/{id}/versions/{versionId}` and hydrates the canvas
- **Activate**: calls `POST /api/workflows/{id}/versions/{versionId}/activate`; toolbar badge updates
- Version badge in toolbar reflects the version currently shown on canvas (may differ from active if a historical version was loaded)

### Execution Details / Timeline

- Workflow execution status header (running / paused / completed / failed).
- Vertical timeline of node executions with timestamps, durations, retry count.
- Click a node → drawer with `input`, `output`, errors, AI usage.

### Approval Inbox

- List of pending approvals: risk level, AI recommendation, contract summary preview.
- Detail view: full AI output, ability to download the source document.
- Actions: Approve / Reject with required comment when rejecting.

### Document Upload

- Drag-and-drop upload area.
- Lists recent uploads; click to use in a workflow execution.

### Node Catalog

- Grid of all node types grouped by category.
- Click → side panel with description, inputs, outputs, configuration schema.

### Settings

- Profile, tenant, AI provider keys (admin), default model.
- (MVP keeps this minimal.)

---

## 4. Data Layer

- All server data flows through TanStack Query keyed on `[entity, params]`.
- API client is generated from the OpenAPI spec so types stay in sync.
- Mutations invalidate the relevant queries; optimistic updates only where safe (e.g. workflow rename).

```tsx
const { data, isLoading } = useQuery({
  queryKey: ['workflows', { page, search }],
  queryFn: () => api.workflows.list({ page, search }),
});
```

---

## 5. Designer Internals

- React Flow stores its own state (`nodes`, `edges`); we mirror to a normalized client model and translate to the canonical workflow JSON only when saving.
- Each node renders as a custom React Flow node bound to its descriptor.
- The configuration form is rendered from `NodeConfigDefinition[]`:
  - `String` → `<Input>` / `<Textarea>` (use `Textarea` when `maxLength` is large or `multiline=true`)
  - `Number` → `<NumberInput>`
  - `Boolean` → `<Switch>`
  - `Enum` (with `allowedValues`) → `<Select>`
  - `Json` → Monaco JSON editor
  - `DocumentRef` → document picker dialog
- Edge conditions are edited in a small expression editor with suggestions for upstream outputs.

---

## 6. Auth Flow

- Login posts to `/api/auth/login`; receives JWT.
- Stored in an HTTP-only cookie via a Next.js route handler.
- App-level layout fetches `me` server-side; unauthenticated users are redirected to `/login`.

---

## 7. Loading / Empty / Error States

Every screen explicitly handles:

- **Loading** — skeletons (not spinners) for lists and detail views.
- **Empty** — illustrated empty state + primary action.
- **Error** — toast + retry button; sensitive errors are sanitized.

---

## 8. Accessibility

- All interactive elements reachable by keyboard.
- Form labels associated with inputs.
- Focus management when dialogs/drawers open.
- Color contrast meets WCAG AA.

---

## 9. Testing

- **Unit:** components with React Testing Library.
- **Integration:** the designer interactions (add/connect/configure node) with mocked API.
- **E2E:** Playwright covering the contract review demo end-to-end.

---

## 10. Theming

- Light mode first; dark mode follows shadcn/ui tokens.
- Brand accent neutral by default; tenants can theme via CSS variables (post-MVP).

---

## 11. Front-End Rules (for AI Coding Agents)

1. The designer renders nodes and forms dynamically from `/api/nodes/catalog`. **Do not hardcode node forms** when descriptor metadata can drive the UI.
2. Keep canvas state separate from persisted definition (only translate on save).
3. Use the generated typed API client; do not hand-write `fetch` calls.
4. Show validation errors before save.
5. Execution and timeline views are **read-only**.
6. Approval actions require a confirmation step (modal).
7. Avoid layout shift: reserve space with skeletons.
8. No `any` in TypeScript; resolve types through the generated client or local Zod schemas.

