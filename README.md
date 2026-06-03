# OrchestFlowAI

<!-- Status & Build -->
[![Status](https://img.shields.io/badge/status-active%20development-yellow?style=flat-square)](https://github.com/ggarciasoft/orchest-flow-ai)
[![Backend Tests](https://img.shields.io/badge/backend%20tests-538%20passing-brightgreen?style=flat-square&logo=dotnet)](./tests/OrchestFlowAI.Tests)
[![Frontend Tests](https://img.shields.io/badge/frontend%20tests-76%20passing-brightgreen?style=flat-square&logo=jest)](./apps/web)
[![Last Commit](https://img.shields.io/github/last-commit/ggarciasoft/orchest-flow-ai?style=flat-square)](https://github.com/ggarciasoft/orchest-flow-ai/commits/main)

<!-- Stack -->
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![Next.js](https://img.shields.io/badge/Next.js-16-000000?style=flat-square&logo=nextdotjs)](https://nextjs.org)
[![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?style=flat-square&logo=typescript)](https://typescriptlang.org)
[![React](https://img.shields.io/badge/React-19-61DAFB?style=flat-square&logo=react)](https://react.dev)

<!-- Project Stats -->
[![Nodes](https://img.shields.io/badge/nodes-24%20built--in-blue?style=flat-square)](./docs/NODES.md)
[![License](https://img.shields.io/badge/license-Apache%202.0-green?style=flat-square)](./LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/ggarciasoft/orchest-flow-ai?style=flat-square)](https://github.com/ggarciasoft/orchest-flow-ai/stargazers)

> Open-source, self-hosted enterprise AI workflow platform for orchestrating agents, tools, human approvals, documents, and business automation.

**GitHub:** https://github.com/ggarciasoft/orchest-flow-ai
**Marketing site:** https://ggarciasoft.github.io/orchest-flow-ai/

OrchestFlowAI is a modular platform that lets teams build AI-driven business workflows using reusable nodes. Workflows are stored as **data**, executed by a backend **engine**, and surfaced through a **visual designer**. You deploy it yourself - no vendor lock-in, no subscription, no data leaving your infrastructure.

---

## 🟢 Current Status

**Active development.** Core architecture is built and running.

| Area | Status |
|---|---|
| Backend API (.NET 9) | ✅ Running - all endpoints implemented |
| Frontend (Next.js) | ✅ Running - designer, pages, auth |
| Marketing site | ✅ Static export → GitHub Pages |
| Workflow Engine | ✅ Implemented - graph execution, retries, approvals |
| Node Library | ✅ **24 nodes** across 6 categories |
| Unit Tests | ✅ **538/538 backend** · **80/80 frontend** passing |
| Database | ✅ PostgreSQL (Docker) — EF Core, auto-migrations |
| Auth guard (frontend) | ✅ Complete - RBAC enforced across all pages and actions |
| Role-gated UI | ✅ Viewers/Approvers cannot trigger write actions anywhere in the UI |
| CI/CD | ✅ GitHub Actions - backend tests, frontend build, Pages deploy |

> See [`BACKLOG.md`](./BACKLOG.md) for the full list of known gaps and next steps.

---

## 🎯 What OrchestFlowAI Does

- **Visual workflow design** - drag-and-drop nodes on a canvas (React Flow). Connect, configure, and delete nodes.
- **Async execution** - a worker service runs workflows, persists state, retries, and resumes after human decisions.
- **24 built-in nodes** - logic, AI, integrations, data, documents, human, and system nodes.
- **AI nodes** - classify, extract, translate, summarize, and analyze using a provider-agnostic LLM abstraction.
- **Human approvals** - first-class node type; workflows pause and resume after approve/reject decisions.
- **Document processing** - PDF text extraction; OCR and classification planned.
- **Integrations** - HTTP requests, email (SMTP), webhooks, Slack - all shipped.
- **Auditability** - full execution timeline, AI usage logs, retries, and decisions recorded.

---

## 🚀 Reference Workflow: Contract Review

```
Upload Contract PDF
  → Extract Text (documents.extract-pdf)
    → AI Contract Risk Analysis (ai.contract-risk-analysis)
      → AI Executive Summary (ai.executive-summary)
        → Condition: Risk == "High" (logic.condition)
          → Human Approval (human.approval)
            → Webhook Notify (integrations.webhook-out)
```

See [`docs/sample-workflows/contract-review.md`](./docs/sample-workflows/contract-review.md).

---

## 🧩 Node Library (21 nodes)

### System
| Type | Purpose |
|---|---|
| `system.start` | Entry point - surfaces workflow inputs |
| `system.end` | Terminal node - marks completion |

### Logic
| Type | Purpose |
|---|---|
| `logic.condition` | Evaluate boolean expression, branch on result |
| `logic.switch` | Route by matching a value against configured cases |
| `logic.delay` | Pause execution for N milliseconds |
| `logic.merge` | Synchronize multiple branches |

### AI
| Type | Purpose |
|---|---|
| `ai.contract-risk-analysis` | Structured contract risk assessment (Low/Medium/High) |
| `ai.executive-summary` | Generate executive summary from text |
| `ai.classify` | Classify text into configured categories |
| `ai.extract` | Extract structured fields from unstructured text |
| `ai.translate` | Translate text to a target language |

### Human
| Type | Purpose |
|---|---|
| `human.approval` | Pause workflow for approve/reject decision |

### Documents
| Type | Purpose |
|---|---|
| `documents.extract-pdf` | Extract plain text from a PDF |

### Integrations
| Type | Purpose |
|---|---|
| `integrations.http` | Call any external REST API |
| `integrations.slack` | Post a message to a Slack webhook |
| `integrations.webhook-out` | POST execution payload to a URL |
| `integrations.email` | Send email via SMTP |

### Data
| Type | Purpose |
|---|---|
| `data.set` | Set variables with `{{placeholder}}` substitution |
| `data.json-transform` | Reshape JSON using dot-notation field mappings |

Full catalog with inputs/outputs/config: [`docs/NODES.md`](./docs/NODES.md)

---

## 🧱 Stack

| Layer | Tech |
|---|---|
| Backend | .NET 9 (C#) - xUnit, Moq, FluentAssertions |
| Frontend | Next.js 16, React 19, TypeScript, Tailwind CSS, React Flow |
| Marketing | Next.js 16 static export → GitHub Pages |
| Package manager | pnpm (workspace) |
| Database | PostgreSQL (Docker) — EF Core, auto-migrations |
| Queue | Redis (StackExchange.Redis) — execution queue + pub/sub |
| AI | Provider abstraction - OpenAI, Anthropic, Azure, Ollama |
| Testing | xUnit + Jest + React Testing Library |
| CI/CD | GitHub Actions |

---

## 📂 Repository Layout

```
orchest-flow-ai/
├── apps/
│   ├── web/                        # Next.js full app (designer, pages, auth)
│   └── marketing/                  # Static marketing site (Next.js, output: export)
├── ui/
│   └── web-public/                 # Shared frontend library (pages, components, content)
├── services/
│   ├── OrchestFlowAI.Api/          # REST API (.NET 9)
│   ├── OrchestFlowAI.Worker/       # Background workflow executor
│   └── OrchestFlowAI.AI/           # LLM abstraction + providers
├── packages/
│   ├── OrchestFlowAI.Domain/       # Entities + domain logic
│   ├── OrchestFlowAI.Application/  # Abstractions (interfaces)
│   ├── OrchestFlowAI.Infrastructure/ # Repos, queue, storage, auth
│   ├── OrchestFlowAI.Contracts/    # Request/response DTOs
│   ├── OrchestFlowAI.Engine/       # Workflow execution engine
│   ├── OrchestFlowAI.SDK/          # Node authoring SDK + test helpers
│   └── OrchestFlowAI.Observability/ # Middleware, logging
├── nodes/
│   ├── ai/                         # AI nodes
│   ├── data/                       # Data transformation nodes
│   ├── documents/                  # Document processing nodes
│   ├── human/                      # Human-in-the-loop nodes
│   ├── integrations/               # External service nodes
│   ├── logic/                      # Control flow nodes
│   └── system/                     # Start/End nodes
├── tests/
│   └── OrchestFlowAI.Tests/        # xUnit test project (538 tests)
├── docs/                           # Architecture, API, nodes, setup docs
├── .github/workflows/              # CI + GitHub Pages deploy
├── docker-compose.yml              # Infra (PostgreSQL, Redis)
├── docker-compose.app.yml          # App (API, Worker, Web)
├── docker-compose.observability.yml # Prometheus, Grafana, OTEL
├── docker-compose.full.yml         # All of the above (single command)
├── pnpm-workspace.yaml             # pnpm workspace (apps/* + ui/*)
├── AGENTS.md                       # AI agent roles and rules
├── BACKLOG.md                      # Known gaps and next steps
└── README.md
```

---

## 📂 Documentation Map

| Doc | Contents |
|---|---|
| [`docs/VISION.md`](./docs/VISION.md) | Product vision, principles, non-goals |
| [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) | System architecture and boundaries |
| [`docs/WORKFLOW-ENGINE.md`](./docs/WORKFLOW-ENGINE.md) | Runtime + execution model |
| [`docs/NODE-SDK.md`](./docs/NODE-SDK.md) | Node authoring guide |
| [`docs/NODES.md`](./docs/NODES.md) | Full node catalog |
| [`docs/AI-RUNTIME.md`](./docs/AI-RUNTIME.md) | LLM abstraction + structured output |
| [`docs/DATABASE.md`](./docs/DATABASE.md) | Data model |
| [`docs/API.md`](./docs/API.md) | REST endpoints |
| [`docs/FRONTEND.md`](./docs/FRONTEND.md) | Web app and designer |
| [`docs/SECURITY.md`](./docs/SECURITY.md) | Auth, tenancy, secrets |
| [`docs/SETUP.md`](./docs/SETUP.md) | Local development setup |
| [`docs/ROADMAP.md`](./docs/ROADMAP.md) | Phases and future work |
| [`BACKLOG.md`](./BACKLOG.md) | Known gaps and next items |
| [`AGENTS.md`](./AGENTS.md) | AI agent roles + unit test enforcement rules |
| [`RULES.md`](./RULES.md) | Coding rules index |

---

## ⚡ Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 22+](https://nodejs.org/)
- [pnpm](https://pnpm.io/installation) (`npm install -g pnpm`)

### Option A - Local dev (no Docker, in-memory DB)
```bash
git clone https://github.com/ggarciasoft/orchest-flow-ai.git
cd orchest-flow-ai

# Backend API (runs at http://localhost:5080 - Swagger at /swagger)
cd services/OrchestFlowAI.Api && dotnet run

# Frontend (runs at http://localhost:3000)
# (from repo root, in a separate terminal)
pnpm install
pnpm run dev:app
```

### Option B - Docker with PostgreSQL
```bash
# Infrastructure only (PostgreSQL + Redis)
docker compose up -d

# Set your connection string and run the API
CONNECTION_STRING="Host=localhost;Database=OrchestFlowAI;Username=OrchestFlowAI;Password=OrchestFlowAI" dotnet run --project services/OrchestFlowAI.Api
```

### Option C - Full Docker stack (infra + app + observability)
```bash
# Copy and configure env vars
cp .env.example .env

# Start everything (postgres, redis, api, worker, web, prometheus, grafana, otel)
docker compose -f docker-compose.full.yml up -d --build
```

| Service | URL |
|---|---|
| Web app | http://localhost:3000 |
| API / Swagger | http://localhost:5080/swagger |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3001 |

For infra + app only (no observability): `docker compose -f docker-compose.yml -f docker-compose.app.yml up -d --build`

### Run tests
```bash
# Backend (538 tests)
cd tests/OrchestFlowAI.Tests && dotnet test --configuration Release

# Frontend (62 tests)
pnpm --filter web test
```

### Marketing site (local dev)
```bash
pnpm run dev:marketing    # http://localhost:3001
pnpm run build:marketing  # static output → apps/marketing/out/
```

> Tables are created automatically on first startup when PostgreSQL is configured - no manual migration step needed.

---

## 🌐 Deployments

| Target | Build command | Output | Hosting |
|---|---|---|---|
| Full app | `pnpm run build:app` | `apps/web/.next/` | Node.js server / Docker |
| Marketing site | `pnpm run build:marketing` | `apps/marketing/out/` | GitHub Pages (auto-deployed on push to `main`) |

The marketing site is live at **https://ggarciasoft.github.io/orchest-flow-ai/**

---

## 🤝 Contributing

Contributions are welcome. Please open an issue or pull request on [GitHub](https://github.com/ggarciasoft/orchest-flow-ai). See [`CONTRIBUTING.md`](./CONTRIBUTING.md) for guidelines.

---

## 📜 License

[Apache 2.0](./LICENSE) - free to use, modify, and distribute, including commercially.
