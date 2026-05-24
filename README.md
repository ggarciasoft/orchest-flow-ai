# OrchestAI

<!-- Status & Build -->
[![Status](https://img.shields.io/badge/status-active%20development-yellow?style=flat-square)](https://github.com/ggarciasoft/orchestai)
[![Backend Tests](https://img.shields.io/badge/backend%20tests-125%20passing-brightgreen?style=flat-square&logo=dotnet)](./tests/OrchestAI.Tests)
[![Frontend Tests](https://img.shields.io/badge/frontend%20tests-24%20passing-brightgreen?style=flat-square&logo=jest)](./apps/web)
[![Last Commit](https://img.shields.io/github/last-commit/ggarciasoft/orchestai?style=flat-square)](https://github.com/ggarciasoft/orchestai/commits/main)

<!-- Stack -->
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![Next.js](https://img.shields.io/badge/Next.js-16-000000?style=flat-square&logo=nextdotjs)](https://nextjs.org)
[![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?style=flat-square&logo=typescript)](https://typescriptlang.org)
[![React](https://img.shields.io/badge/React-19-61DAFB?style=flat-square&logo=react)](https://react.dev)

<!-- Project Stats -->
[![Nodes](https://img.shields.io/badge/nodes-19%20built--in-blue?style=flat-square)](./docs/NODES.md)
[![License](https://img.shields.io/badge/license-Apache%202.0-green?style=flat-square)](./LICENSE)

<!-- TODO: replace static test badges with dynamic CI badges once GitHub Actions is configured -->

> Open-source enterprise AI workflow platform for orchestrating agents, tools, human approvals, documents, and business automation.

OrchestAI is a modular platform that lets teams build AI-driven business workflows using reusable nodes. Workflows are stored as **data**, executed by a backend **engine**, and surfaced through a **visual designer**.

---

## 🟢 Current Status

**Active development.** Core architecture is built and running.

| Area | Status |
|---|---|
| Backend API (.NET 9) | ✅ Running — all endpoints implemented |
| Frontend (Next.js) | ✅ Running — designer, pages, auth |
| Workflow Engine | ✅ Implemented — graph execution, retries, approvals |
| Node Library | ✅ **19 nodes** across 6 categories |
| Unit Tests | ✅ **125/125 backend** · **24/24 frontend** passing |
| Database | ⚠️ In-memory stubs (PostgreSQL not yet wired) |
| Designer Save | ⚠️ Not yet implemented — see `BACKLOG.md` |
| Auth guard (frontend) | ⚠️ Partial — login works, route guard pending |

> See [`BACKLOG.md`](./BACKLOG.md) for the full list of known gaps and next steps.

---

## 🎯 What OrchestAI Does

- **Visual workflow design** — drag-and-drop nodes on a canvas (React Flow). Connect, configure, and delete nodes.
- **Async execution** — a worker service runs workflows, persists state, retries, and resumes after human decisions.
- **19 built-in nodes** — logic, AI, integrations, data, documents, human approval, and system nodes.
- **AI nodes** — classify, extract, translate, summarize, and analyze using a provider-agnostic LLM abstraction.
- **Human approvals** — first-class node type; workflows pause and resume after approve/reject decisions.
- **Document processing** — PDF text extraction; OCR and classification planned.
- **Integrations** — HTTP requests, email (SMTP), webhooks, Slack — all shipped.
- **Auditability** — full execution timeline, AI usage logs, retries, and decisions recorded.

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

## 🧩 Node Library (19 nodes)

### System
| Type | Purpose |
|---|---|
| `system.start` | Entry point — surfaces workflow inputs |
| `system.end` | Terminal node — marks completion |

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
| Backend | .NET 9 (C#) — xUnit, Moq, FluentAssertions |
| Frontend | Next.js 16, React 19, TypeScript, Tailwind CSS, React Flow |
| Database | PostgreSQL *(planned — currently in-memory stubs)* |
| Queue | In-memory .NET Channels *(Redis/Service Bus planned)* |
| AI | Provider abstraction — OpenAI, FakeLLMProvider (tests) |
| Testing | xUnit + Jest + React Testing Library |

---

## 📂 Repository Layout

```
orchestai/
├── apps/
│   └── web/                    # Next.js frontend (designer, pages, auth)
├── services/
│   ├── OrchestAI.Api/          # REST API (.NET 9)
│   ├── OrchestAI.Worker/       # Background workflow executor
│   └── OrchestAI.AI/           # LLM abstraction + providers
├── packages/
│   ├── OrchestAI.Domain/       # Entities + domain logic
│   ├── OrchestAI.Application/  # Abstractions (interfaces)
│   ├── OrchestAI.Infrastructure/ # Repos, queue, storage, auth
│   ├── OrchestAI.Contracts/    # Request/response DTOs
│   ├── OrchestAI.Engine/       # Workflow execution engine
│   ├── OrchestAI.SDK/          # Node authoring SDK + test helpers
│   └── OrchestAI.Observability/ # Middleware, logging
├── nodes/
│   ├── ai/                     # AI nodes
│   ├── data/                   # Data transformation nodes
│   ├── documents/              # Document processing nodes
│   ├── human/                  # Human-in-the-loop nodes
│   ├── integrations/           # External service nodes
│   ├── logic/                  # Control flow nodes
│   └── system/                 # Start/End nodes
├── tests/
│   └── OrchestAI.Tests/        # xUnit test project (125 tests)
├── docs/                       # Architecture, API, nodes, setup docs
├── rules/                      # Coding rules by domain
├── AGENTS.md                   # AI agent roles and rules
├── BACKLOG.md                  # Known gaps and next steps
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

### Option A — Local dev (no Docker, in-memory DB)
```bash
git clone https://github.com/ggarciasoft/orchestai.git
cd orchestai

# Backend API (runs at http://localhost:5080 — Swagger at /swagger)
cd services/OrchestAI.Api && dotnet run

# Frontend (runs at http://localhost:3000)
cd apps/web && npm install && npm run dev
```

### Option B — Docker with PostgreSQL
```bash
# Infrastructure only (PostgreSQL + Redis)
docker compose up -d

# Set your connection string and run the API
CONNECTION_STRING="Host=localhost;Database=orchestai;Username=orchestai;Password=orchestai" dotnet run --project services/OrchestAI.Api
```

### Option C — Full Docker stack
```bash
# Copy and configure env vars
cp .env.example .env

# Start infra + app
docker compose -f docker-compose.yml -f docker-compose.app.yml up -d --build
```

### Run tests
```bash
# Backend (125 tests)
cd tests/OrchestAI.Tests && dotnet test

# Frontend (26 tests)
cd apps/web && npm test
```

> Tables are created automatically on first startup when PostgreSQL is configured — no manual migration step needed.

---

## 📜 License

TBD (suggested: Apache 2.0 for OSS-friendly enterprise use).
