# Vision

## Product Vision

OrchestAI is an open-source enterprise AI workflow platform. It lets organizations orchestrate AI agents, tools, document processing, human approvals, and integrations through reusable, composable nodes.

The platform treats workflows as **data**, not code. A workflow definition is a graph (nodes + edges + configuration) that the engine executes. Nodes are reusable, stateless units of work that can be authored independently by teams or third parties.

The long-term goal is an extensible enterprise workflow ecosystem — similar in spirit to Zapier, n8n, or Camunda — but **AI-native**, with first-class support for LLM reasoning, structured outputs, agent loops, RAG, and human-in-the-loop oversight.

## Why It Matters

Most "AI app" tools today either:

- Wrap a chatbot around an LLM (no orchestration, no audit, no enterprise integration), or
- Force engineers to wire up bespoke code per use case (no reuse, no visual design, no business-user accessibility).

OrchestAI sits between those poles: **a workflow runtime where AI is one node type among many**, with the auditability, modularity, and governance enterprises require.

## Core Principles

### 1. Modular First

The platform is built from clean, separable modules:

- **Engine** — workflow runtime (graph traversal, state, retries)
- **Node SDK** — interfaces and metadata for authoring nodes
- **Node Registry** — discovery and listing of installed nodes
- **Designer** — visual canvas
- **Worker** — async execution
- **AI Runtime** — LLM provider abstraction
- **Connectors** — integrations
- **Observability** — logs, traces, metrics

The engine **must not** know implementation details of any specific node.

### 2. Workflows Are Data

A workflow is a structured definition containing nodes, edges, inputs, outputs, configuration, version, and metadata. It is **never** hardcoded into application logic. Workflows can be created, versioned, validated, and executed entirely through data.

### 3. Nodes Are Reusable Components

Each node represents a reusable unit of work — AI Summarize, Extract PDF Text, Human Approval, Send Email, HTTP Request, Condition, Delay, Webhook Trigger, etc. Nodes are stateless and isolated; the engine owns execution state.

### 4. Human-in-the-Loop Is First-Class

Enterprise workflows often require human review. Approval and manual-review nodes are part of the MVP, with workflows able to pause and resume around them.

### 5. Auditability Matters

Every execution is traceable: trigger, timing, per-node input/output, model used, token usage, errors, retries, approval decisions.

### 6. AI Must Produce Structured Outputs

For business decisions, AI nodes prefer structured JSON outputs with schemas — not free-form text. Confidence and risk are surfaced where relevant.

### 7. Start Practical, Then Expand

The MVP focuses on **one** strong business workflow — Contract Review — to prove the architecture end-to-end. Expansion is roadmap-driven, not speculative.

## Target Users

- **Business operations teams** building review/approval workflows.
- **AI/ML engineers** integrating LLMs into business processes with proper audit.
- **Platform engineers** standing up an internal workflow platform.
- **ISV / consultancies** building vertical AI workflows for customers.

## Non-Goals (MVP)

OrchestAI **will not** ship the following in MVP. They are deliberately deferred:

- Full plugin marketplace
- Complex multi-agent orchestration
- Full RAG platform
- Dozens of integrations
- Kubernetes deployment
- Enterprise SSO (SAML/OIDC)
- Billing/metering
- Advanced analytics dashboards
- AI-generated workflows from natural language
- Public plugin ecosystem

See [ROADMAP.md](./ROADMAP.md) for when each lands.

## North Star

> A new developer can clone the repo, run `docker compose up`, upload a contract PDF, watch a structured AI risk analysis run, approve a high-risk contract, and inspect the full execution timeline — within 10 minutes.
