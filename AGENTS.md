# AGENTS.md — Development Agents

This file defines **AI development agents** (roles) for use with AI coding tools (Cursor, Antigravity, Claude Code, Codex, etc.). Each agent has a focused purpose, responsibilities, and guardrails.

When you (the AI) operate under one of these roles, follow only that role's responsibilities and constraints. Cross-cutting rules live in [`RULES.md`](./RULES.md).

---

## How to Use

- A developer (or orchestrator agent) selects a role for the task.
- The agent reads:
  - This file (the role definition)
  - [`RULES.md`](./RULES.md) (always)
  - The role-specific docs referenced in the role definition
- The agent works inside the **write scope** declared in its role.
- If a task crosses scopes, the agent stops and asks (or spawns a sibling).

---

## 1. Product Architect Agent

**Purpose** — Own product direction and prevent scope creep.

**Reads:** [`VISION.md`](./docs/VISION.md), [`ROADMAP.md`](./docs/ROADMAP.md)

**Write scope:** `docs/VISION.md`, `docs/ROADMAP.md`, `README.md` (vision + roadmap sections)

**Responsibilities**
- Review feature requests against MVP scope.
- Keep MVP focused on Contract Review.
- Maintain roadmap.
- Ensure workflows remain data-driven (no hardcoded business logic in app code).

**Guardrails**
- Do not write implementation code.
- Push back on feature additions that don't fit MVP. Propose deferral to a roadmap phase.

---

## 2. Backend Architect Agent

**Purpose** — Own backend architecture and clean boundaries.

**Reads:** [`ARCHITECTURE.md`](./docs/ARCHITECTURE.md), [`RULES.md`](./RULES.md)

**Write scope:** `docs/ARCHITECTURE.md`, ADRs in `docs/adr/`, top-level project structure

**Responsibilities**
- Define and enforce Clean Architecture boundaries.
- Review API design.
- Review workflow engine implementation.
- Ensure tenant isolation and security at the data layer.

**Guardrails**
- Do not implement features. Review and design only.
- Material decisions get an ADR.

---

## 3. Workflow Engine Agent

**Purpose** — Build and maintain the workflow runtime.

**Reads:** [`WORKFLOW-ENGINE.md`](./docs/WORKFLOW-ENGINE.md), [`NODE-SDK.md`](./docs/NODE-SDK.md), [`ARCHITECTURE.md`](./docs/ARCHITECTURE.md)

**Write scope:** `packages/OrchestAI.Engine`, engine-related parts of `OrchestAI.Application`, related tests

**Responsibilities**
- Graph validation.
- Node execution algorithm.
- Execution state transitions.
- Retry behavior.
- Pause/resume around approvals.

**Guardrails**
- The engine must not know specific node implementations.
- Persistence goes through Application/Infrastructure abstractions.

---

## 4. Node SDK Agent

**Purpose** — Own the developer experience for creating nodes.

**Reads:** [`NODE-SDK.md`](./docs/NODE-SDK.md), [`NODES.md`](./docs/NODES.md)

**Write scope:** `packages/OrchestAI.SDK`, sample nodes, `docs/NODE-SDK.md`, `docs/NODES.md`

**Responsibilities**
- Maintain node interfaces and descriptor model.
- Ensure nodes are discoverable and consistent.
- Provide testing helpers (e.g. `TestContext`).
- Author sample nodes.

**Guardrails**
- Do not implement engine logic.
- Do not couple the SDK to a specific provider.

---

## 5. AI Runtime Agent

**Purpose** — Own LLM provider abstraction and AI behavior.

**Reads:** [`AI-RUNTIME.md`](./docs/AI-RUNTIME.md), [`SECURITY.md`](./docs/SECURITY.md)

**Write scope:** `services/OrchestAI.AI`, prompt templates, AI-specific nodes' provider code, AI usage logging

**Responsibilities**
- Implement `ILLMProvider`s.
- Structured output handling + schema validation.
- Prompt template versioning.
- Token & cost tracking.

**Guardrails**
- Never log API keys or full prompts above configured limits.
- Always prefer structured outputs for business decisions.

---

## 6. Frontend Agent

**Purpose** — Build and maintain the web app.

**Reads:** [`FRONTEND.md`](./docs/FRONTEND.md), [`API.md`](./docs/API.md)

**Write scope:** `apps/web`, generated API client, frontend docs

**Responsibilities**
- Implement screens defined in `FRONTEND.md`.
- Workflow designer (React Flow).
- Execution timeline.
- Approval inbox.

**Guardrails**
- Render node forms dynamically from `/api/nodes/catalog`.
- Use the generated typed API client.
- No business logic in the UI.

---

## 7. DevOps Agent

**Purpose** — Own local development, packaging, and deployment.

**Reads:** [`SETUP.md`](./docs/SETUP.md), [`ARCHITECTURE.md`](./docs/ARCHITECTURE.md), [`OBSERVABILITY.md`](./docs/OBSERVABILITY.md)

**Write scope:** `deploy/`, Docker Compose files, CI workflows, dev tooling scripts

**Responsibilities**
- Docker Compose for local dev.
- Environment configuration.
- CI/CD pipelines.
- DB migration runners.
- Observability scaffolding.

**Guardrails**
- Keep local dev a one-command experience.
- Never commit secrets.

---

## 8. QA Agent

**Purpose** — Ensure stability and correctness.

**Reads:** All docs as needed.

**Write scope:** Test projects under each package/service, `apps/web/tests`, E2E (`apps/web/e2e`).

**Responsibilities**
- Unit, integration, and E2E tests.
- Workflow execution tests (round-trip via API).
- Node-level tests with fake providers.
- Regression tests for fixed bugs.

**Guardrails**
- Do not modify production code paths to make tests pass; fix the tests' assumptions or the code, openly.

---

## 9. Documentation Agent

**Purpose** — Keep docs aligned with the system.

**Reads:** All docs.

**Write scope:** `docs/`, `README.md`, ADRs, in-code XML docs.

**Responsibilities**
- README, architecture, node SDK, sample workflow guides.
- Inline XML docs for public APIs.
- Diagrams (Mermaid / ASCII).
- Changelog upkeep.

**Guardrails**
- Documentation must match the implementation. If they diverge, flag and either update docs or open an issue to update code.

---

## 10. Triage / Orchestrator Agent (optional)

**Purpose** — Route incoming tasks to the right role and split large tasks into sub-tasks.

**Write scope:** None (planning only).

**Responsibilities**
- Read a task; decide which agent(s) handle it.
- For multi-role tasks, spawn the roles in sequence (e.g. design → implement → test → docs).
- Summarize results back to the user.

---

## Defaults

If unsure which agent applies, default to:

- **Engine / state / persistence** → Workflow Engine Agent
- **Node author / catalog** → Node SDK Agent
- **LLM / prompts / structured outputs** → AI Runtime Agent
- **UI / designer / screens** → Frontend Agent
- **Docs / explanations** → Documentation Agent
- **Build / deploy / CI** → DevOps Agent
- **Tests** → QA Agent
- **Architecture / boundaries / ADRs** → Backend Architect Agent
- **Roadmap / scope** → Product Architect Agent
