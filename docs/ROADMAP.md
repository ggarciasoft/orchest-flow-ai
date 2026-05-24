# Roadmap

OrchestFlowAI is delivered in numbered phases. Each phase has deliverables and acceptance criteria. **MVP = Phases 0–8.** Future phases are roadmap, not commitments.

---

## Phase 0 — Repository Setup

**Deliverables**
- Monorepo skeleton.
- Top-level README + docs folder.
- `OrchestFlowAI.sln` and base project structure.
- Docker Compose skeleton.
- Coding conventions, `.editorconfig`, lint configs.
- CI pipeline (build + test + lint).

**Acceptance**
- Repo builds locally.
- Docs explain project purpose.
- New developer can run `dotnet build` and `pnpm build` without errors.

---

## Phase 1 — Domain and Workflow Schema

**Deliverables**
- Domain entities (`Workflow`, `WorkflowVersion`, `WorkflowNode`, `WorkflowEdge`, executions, approvals, document, AI usage, audit log, tenant, user).
- Workflow definition JSON model + validator.
- Initial DB schema and migrations.

**Acceptance**
- A workflow can be represented as JSON.
- Workflow definition validation runs.
- Workflow/version can be persisted.

---

## Phase 2 — Node SDK and Registry

**Deliverables**
- `IWorkflowNode`, `IWorkflowNodeDescriptor`, port/config definitions.
- Node registry + DI registration patterns.
- `GET /api/nodes/catalog` endpoint.
- `system.start`, `system.end` implementations.

**Acceptance**
- API can list available nodes.
- Nodes can be registered from any package.
- Frontend can consume node metadata.

---

## Phase 3 — Workflow Execution Engine

**Deliverables**
- Execution creation flow.
- Sequential node execution algorithm.
- Input/output mapping along edges.
- Condition expression evaluator.
- State persistence (`WorkflowExecution`, `NodeExecution`).
- Retry handling skeleton.

**Acceptance**
- A simple workflow executes from start to end.
- Each node execution is persisted.
- Failed nodes mark workflow as failed.

---

## Phase 4 — Document and AI MVP Nodes

**Deliverables**
- `document.extract-pdf-text`.
- `ai.contract-risk-analysis` with JSON schema.
- `ai.executive-summary`.
- `ILLMProvider` abstraction + OpenAI implementation.
- `ai_usage_logs` recording.

**Acceptance**
- Uploaded contract → AI returns structured analysis.
- AI usage is logged.

---

## Phase 5 — Human Approval

**Deliverables**
- `human.approval` node.
- `approval_requests` table + APIs (`approve`/`reject`).
- Pause/resume in the engine.

**Acceptance**
- Workflow pauses for approval.
- User can approve/reject through the API.
- Workflow resumes correctly.

---

## Phase 6 — Frontend MVP

**Deliverables**
- Auth + layout.
- Dashboard, workflows list, basic designer (read-mostly with node placement + config drawer), execution details, approval inbox, document upload.

**Acceptance**
- User can upload contract.
- User can execute contract review workflow.
- User can view execution timeline.
- User can approve/reject high-risk workflow.

---

## Phase 7 — Dockerized Demo

**Deliverables**
- Compose file with Postgres, Redis, API, Worker, Web.
- Seed of sample workflow + sample contract PDF.

**Acceptance**
- New developer runs `docker compose up` and the demo works end-to-end.

---

## Phase 8 — Documentation & Public GitHub Polish

**Deliverables**
- README with screenshots/GIFs.
- Architecture, node SDK, contract-review demo guide.
- Roadmap, contribution guide, license.

**Acceptance**
- Repo looks professional.
- A new developer understands how to run and extend the platform.

---

# Future Phases

## Phase 9 — Visual Designer Improvements

- Drag-and-drop polish, dynamic config forms, validation UI, search, categories, version comparison.

## Phase 10 — Advanced Logic Nodes

- `logic.switch`, `logic.loop`, `logic.parallel`, `logic.delay`, `logic.retry-policy`, error handler.

## Phase 11 — Integrations Pack

- `integration.email.send`, `integration.http.request`, `integration.webhook.call`, `integration.slack.send-message`, `integration.teams.send-message`, `integration.jira.create-ticket`, additional AI/document nodes.

## Phase 12 — RAG / Knowledge Base

- Document ingestion, embeddings, pgvector store, retrieval node, citations, KB management UI.

## Phase 13 — Agent System

- Agent configuration, tools, memory, `ai.agent-executor`, multi-agent workflow support.

## Phase 14 — Enterprise Security

- SSO (OIDC / SAML), RBAC, per-node permissions, secrets vault integration, tenant policies.

## Phase 15 — Observability & Cost Management

- Cost dashboards, token dashboards, execution analytics, node performance metrics, failure analysis.

## Phase 16 — Developer Ecosystem

- Node package format, plugin loading, node SDK docs, sample custom nodes, marketplace concept.

## Phase 17 — AI Workflow Generator

- Natural-language → draft workflow definition (NL2Workflow).

---

# Definition of Done for MVP

The MVP is done when **all** of the following are true:

- A developer can run the full platform locally with one command.
- A user can upload a contract PDF.
- A predefined contract review workflow can execute.
- AI returns structured risk analysis.
- A high-risk contract creates a human approval request.
- The user can approve or reject.
- Execution timeline is visible end-to-end.
- Node execution history is persisted.
- AI usage is logged with cost estimates.
- README explains how to run the demo.
- Architecture docs explain how to add a new node.

---

# Explicit Non-Goals (MVP)

Do **not** include these in MVP:

- Full marketplace
- Multi-agent system
- Full RAG platform
- Dozens of integrations
- Kubernetes deployment
- SSO
- Billing
- Advanced analytics
- AI-generated workflows
- Public plugin ecosystem
