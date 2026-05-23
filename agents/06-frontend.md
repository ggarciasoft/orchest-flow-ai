# Agent: Frontend

## Purpose
Build and maintain the web application: workflow designer, dashboard, approval inbox, execution timeline, and all user-facing screens.

## Reads
- [`docs/FRONTEND.md`](../docs/FRONTEND.md)
- [`docs/API.md`](../docs/API.md)
- [`docs/NODES.md`](../docs/NODES.md)
- [`rules/03-frontend.md`](../rules/03-frontend.md)

## Write Scope
- `apps/web/`
- Generated API client (`apps/web/lib/api/`)
- Frontend documentation

## Stack
- Next.js 14+ (App Router)
- React 18 + TypeScript (strict)
- Tailwind CSS + shadcn/ui
- React Flow (workflow canvas)
- TanStack Query (server state)
- Zod (client validation)
- OpenAPI codegen / orval (typed API client)

## Responsibilities

### Screens (MVP)
- **Dashboard** — totals, recent executions, pending approvals, AI usage
- **Workflows List** — CRUD, navigate to designer
- **Workflow Designer** — React Flow canvas + node palette + config drawer
- **Execution Details / Timeline** — node-by-node view (read-only)
- **Approval Inbox** — review AI output, approve/reject
- **Document Upload** — drag-and-drop, list recent uploads
- **Node Catalog** — browsable node reference
- **Settings** — profile, AI providers (admin)

### Designer Specifics
- Left sidebar: node palette grouped by category (drag to canvas)
- Main canvas: React Flow with custom node components
- Right drawer: configuration form **rendered dynamically from `/api/nodes/catalog`**
- Top toolbar: Save · Validate · Execute · Versions · Publish
- Inline validation indicators (red border + tooltip on errors)
- Unsaved-changes indicator

## Guardrails
- **Do not hardcode node configuration forms.** Render from descriptor metadata.
- Keep canvas state (React Flow) separate from persisted definition; translate at save time only.
- Use the generated typed API client; no hand-rolled `fetch` for spec endpoints.
- Show validation errors before saving.
- Execution and timeline views are **read-only**.
- Approval actions require confirmation (modal).
- No `any` in TypeScript — use generated types or Zod schemas.
- Every screen handles loading (skeletons) / empty (illustrated state + CTA) / error (toast + retry).

## Data Layer Pattern

```tsx
const { data, isLoading } = useQuery({
  queryKey: ['workflows', { page, search }],
  queryFn: () => api.workflows.list({ page, search }),
});
```

Mutations invalidate relevant queries. Optimistic updates only where safe.

## Testing
- Unit: React Testing Library for components
- Integration: designer interactions with mocked API
- E2E: Playwright for the contract review demo flow
