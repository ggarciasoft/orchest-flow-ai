# OrchestAI

> Open-source enterprise AI workflow platform for orchestrating agents, tools, human approvals, documents, and business automation.

OrchestAI is a modular platform that lets teams build AI-driven business workflows using reusable nodes. Workflows are stored as **data**, executed by a backend **engine**, and surfaced through a **visual designer**.

---

## 🎯 What OrchestAI Does

- **Visual workflow design** — connect reusable nodes on a canvas (React Flow).
- **Async execution** — a worker service runs workflows, persists state, retries, and resumes.
- **AI nodes** — summarize, classify, extract, analyze, reason, and call tools through a provider-agnostic LLM abstraction.
- **Human approvals** — first-class node type; workflows pause and resume.
- **Document processing** — PDF text extraction, OCR (future), classification.
- **Integrations** — email, HTTP, webhooks, Slack, Teams, Jira (post-MVP).
- **Auditability** — full execution timeline, AI usage logs, retries, decisions.

---

## 🚀 MVP: Contract Review Workflow

The MVP proves the architecture by shipping a single end-to-end flow:

```
Upload Contract PDF
  → Extract Text
    → AI Analyze Contract Risk
      → AI Generate Executive Summary
        → If Risk Is High → Human Approval
          → Generate Final Report
            → Complete
```

See [`docs/sample-workflows/contract-review.md`](./docs/sample-workflows/contract-review.md).

---

## 🧱 Stack

| Layer       | Tech                                                                |
| ----------- | ------------------------------------------------------------------- |
| Backend     | .NET 9 (C#)                                                         |
| Frontend    | Next.js, React, TypeScript, Tailwind, shadcn/ui, React Flow         |
| Database    | PostgreSQL                                                          |
| Cache/Queue | Redis (optional for MVP)                                            |
| AI          | Provider abstraction (OpenAI, Azure OpenAI, Anthropic)              |
| Deploy      | Docker Compose locally; Kubernetes/Terraform later                  |
| Observ.     | OpenTelemetry, structured logs, correlation IDs                     |

---

## 📂 Documentation Map

Start here:

1. [`docs/VISION.md`](./docs/VISION.md) — product vision, principles, non-goals
2. [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) — system architecture
3. [`docs/WORKFLOW-ENGINE.md`](./docs/WORKFLOW-ENGINE.md) — runtime + execution model
4. [`docs/NODE-SDK.md`](./docs/NODE-SDK.md) — node contracts and authoring guide
5. [`docs/NODES.md`](./docs/NODES.md) — node catalog (MVP + future)
6. [`docs/AI-RUNTIME.md`](./docs/AI-RUNTIME.md) — LLM abstraction + structured output
7. [`docs/DATABASE.md`](./docs/DATABASE.md) — data model
8. [`docs/API.md`](./docs/API.md) — REST endpoints
9. [`docs/FRONTEND.md`](./docs/FRONTEND.md) — web app and designer
10. [`docs/SECURITY.md`](./docs/SECURITY.md) — auth, tenancy, secrets
11. [`docs/OBSERVABILITY.md`](./docs/OBSERVABILITY.md) — logs, traces, metrics
12. [`docs/SETUP.md`](./docs/SETUP.md) — local development
13. [`docs/ROADMAP.md`](./docs/ROADMAP.md) — phases and future work
14. [`docs/GLOSSARY.md`](./docs/GLOSSARY.md) — vocabulary

For contributors / coding agents:

- [`AGENTS.md`](./AGENTS.md) — development agents (roles for AI coding tools)
- [`RULES.md`](./RULES.md) — coding rules and architectural constraints
- [`CONTRIBUTING.md`](./CONTRIBUTING.md) — how to contribute
- [`docs/PROMPTS.md`](./docs/PROMPTS.md) — bootstrap prompts for AI tools

---

## 📦 Repository Layout

```
orchestai/
├── apps/
│   ├── web/                    # Next.js frontend
│   └── docs/                   # Docs site (optional)
├── services/
│   ├── OrchestAI.Api/          # REST API
│   ├── OrchestAI.Worker/       # Background workflow executor
│   └── OrchestAI.AI/           # AI runtime (service or module)
├── packages/
│   ├── OrchestAI.Domain/
│   ├── OrchestAI.Application/
│   ├── OrchestAI.Infrastructure/
│   ├── OrchestAI.Contracts/
│   ├── OrchestAI.Engine/
│   ├── OrchestAI.SDK/
│   └── OrchestAI.Observability/
├── nodes/
│   ├── ai/
│   ├── documents/
│   ├── human/
│   ├── logic/
│   ├── integrations/
│   └── system/
├── deploy/
│   ├── docker-compose.yml
│   ├── k8s/
│   └── terraform/
├── samples/
│   └── contract-review-workflow/
├── docs/
└── README.md
```

See [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) for the rationale behind each project.

---

## 🟢 Status

**Pre-MVP planning.** No code yet. Documentation-first to lock the architecture before implementation.

---

## 📜 License

TBD (suggested: Apache 2.0 for OSS-friendly enterprise use).
