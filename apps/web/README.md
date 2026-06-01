# OrchestFlowAI — Web App

The full authenticated web application: workflow designer, dashboard, forms, executions, approvals, settings, and all administrative screens.

> This is one of two frontend apps. See also [`apps/marketing/`](../marketing/) (static marketing site).

## Quick Start

From the **repo root**:

```bash
pnpm install
pnpm run dev:app
```

The app starts at **http://localhost:3000**.

> The backend API must be running at `http://localhost:5080` (the default). See the [root README](../../README.md) for full setup instructions.

## Tech Stack

| What | Detail |
|---|---|
| Framework | Next.js 16 (App Router) |
| Language | TypeScript 5 |
| Styling | Tailwind CSS v4 |
| State | React Query, React Context |
| Designer | React Flow (`@xyflow/react`) |
| Real-time | SignalR (`@microsoft/signalr`) |
| Tests | Jest + React Testing Library |

## Project Structure

```
src/
├── app/
│   ├── (app)/              # Authenticated routes (sidebar layout)
│   │   ├── dashboard/
│   │   ├── workflows/
│   │   ├── executions/
│   │   ├── forms/
│   │   ├── approvals/
│   │   ├── documents/
│   │   ├── playground/
│   │   ├── settings/
│   │   └── onboarding/
│   ├── docs/               # Public docs pages (re-exported from @orchest-flow-ai/web-public)
│   ├── login/
│   ├── signup/
│   ├── terms/              # Re-exported from @orchest-flow-ai/web-public
│   ├── privacy/            # Re-exported from @orchest-flow-ai/web-public
│   ├── feedback/           # Re-exported from @orchest-flow-ai/web-public
│   ├── page.tsx            # Home page (re-exported from @orchest-flow-ai/web-public)
│   ├── layout.tsx          # Root layout (providers, cookie banner)
│   └── globals.css
├── components/
│   ├── designer/           # Workflow designer (canvas, nodes, drawers)
│   ├── providers.tsx       # React Query + Auth providers
│   └── ...
├── contexts/               # AuthContext
├── hooks/                  # useExecutionStream, etc.
├── lib/
│   ├── api.ts              # Backend API client
│   └── auth.ts             # JWT utilities
└── content/docs/           # Docs index (shared via @orchest-flow-ai/web-public)
```

## Shared Pages

Public pages (home, docs, terms, privacy, feedback) are defined once in [`ui/web-public/`](../../ui/web-public/) and re-exported here as thin wrappers. This keeps them in sync with `apps/marketing/`.

## Auth & RBAC

- Client-side auth via JWT in `localStorage`
- `AuthContext` provides `isAdmin`, `canEdit`, `isApprover` flags
- All write actions (save, run, delete, approve, etc.) are gated by role
- Roles: `Admin`, `Editor`, `Approver`, `Viewer` (PascalCase, matching backend)

## Scripts

| Script | Description |
|---|---|
| `pnpm run dev:app` | Start dev server on port 3000 (from repo root) |
| `pnpm run build:app` | Production build |
| `pnpm --filter web test` | Run Jest tests |
| `pnpm --filter web lint` | Run ESLint |

## Build

```bash
pnpm run build:app
```

Produces a standard Next.js build in `.next/`. Deploy as a Node.js server or Docker container.

## Related

- [Root README](../../README.md) — full project overview and setup
- [Marketing site](../marketing/) — static export for GitHub Pages
- [Shared UI package](../../ui/web-public/) — shared pages and components
- [Frontend docs](../../docs/FRONTEND.md) — screens, RBAC, and architecture details
