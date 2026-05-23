# Bootstrap Prompts for AI Coding Tools

Drop these prompts into Cursor, Antigravity, Claude Code, Codex, or any AI coding tool to kick off work on OrchestAI consistently with the architecture.

---

## 1. Senior Architect / Full-Stack Bootstrap

```
You are a senior software architect and full-stack engineer helping build OrchestAI
(open-source enterprise AI workflow platform). The system lets users create custom
workflows using reusable nodes. Workflows are stored as data, executed by a backend
workflow engine, and can include AI, document, logic, integration, and human approval
nodes.

Build the system with these principles:

1. Modular monorepo structure.
2. Workflows are data — never hardcoded in business logic.
3. Workflows are graphs (nodes + edges) with versioned definitions.
4. The engine executes nodes through interfaces; it must not know specific node
   implementations.
5. Nodes are reusable, stateless components.
6. The engine owns execution state, retries, and transitions.
7. The frontend designer renders nodes dynamically from backend metadata.
8. AI provider integration is abstracted via ILLMProvider.
9. AI nodes prefer structured JSON outputs validated by schema.
10. Human approval is a first-class workflow step.
11. Execution history, audit logs, and AI usage tracking are required.
12. MVP scope is the Contract Review Workflow only.

MVP workflow:

Upload Contract PDF
→ Extract Text
→ AI Analyze Contract Risk
→ AI Generate Executive Summary
→ If Risk Is High → Human Approval
→ Generate Final Report
→ Complete

Recommended stack:

Backend:  .NET 9
Frontend: Next.js, React, TypeScript, Tailwind, shadcn/ui, React Flow
Database: PostgreSQL
Cache/Queue: Redis (optional MVP)
AI: OpenAI / Azure OpenAI / Anthropic via provider abstraction
Deployment: Docker Compose for local

Start by creating:

- Monorepo structure (apps/, services/, packages/, nodes/, deploy/, samples/, docs/).
- Backend solution + project skeletons.
- Domain entities and workflow definition model.
- Node SDK interfaces (IWorkflowNode, IWorkflowNodeDescriptor).
- Node registry + /api/nodes/catalog endpoint.
- Basic API endpoints (workflows, executions, approvals, documents).
- Worker skeleton with execution loop and pause/resume.
- Frontend app skeleton with the workflow designer page.
- Initial documentation.

Rules:
- Do not invent external integrations that aren’t implemented.
- Mark all future features clearly as roadmap items.
- Keep secrets out of workflow JSON and logs.
- Tenant isolation must be enforced at the repository layer.
```

---

## 2. UI/UX Design Prompt

```
Design a modern enterprise SaaS web application UI for a product called OrchestAI.

OrchestAI is an AI workflow automation platform where business users and technical
teams can create workflows using visual nodes. Workflows include AI analysis,
document processing, conditions, human approvals, and integrations.

The UI should feel premium, modern, clean, and enterprise-ready. Not a playful
chatbot app — a serious workflow automation and AI operations platform.

Design style:

- Clean SaaS dashboard
- Modern enterprise AI aesthetic
- Minimal but powerful
- Light mode first, with dark mode compatibility
- Rounded cards, soft shadows, clear hierarchy, generous spacing
- Professional typography
- Subtle AI/automation visual language

Core screens:

1. Dashboard (totals, recent executions, pending approvals, failed executions, AI usage summary)
2. Workflows List
3. Workflow Designer (the hero screen)
4. Node Catalog Panel
5. Node Configuration Drawer
6. Execution Details / Timeline
7. Approval Inbox
8. Contract Review Demo Page
9. Settings / AI Providers

Most important screen — Workflow Designer:

- Left sidebar with node categories (AI, Documents, Logic, Human, Integrations, System)
- Main React Flow canvas with connected workflow nodes
- Right configuration drawer for selected node
- Top toolbar: Save · Validate · Execute · Versions · Publish
- Inline validation indicators
- Mini-map / zoom controls

Example workflow to render in the design:

Start → Upload Contract PDF → Extract Text → AI Risk Analysis →
Executive Summary → Condition (Risk High) → Human Approval → End

Execution Details screen shows:
- Workflow status
- Node-by-node timeline with timestamps and durations
- Inputs/outputs per node
- AI token usage
- Errors and retries
- Approval decision history

Approval Inbox shows:
- Pending approvals with risk level
- AI recommendation
- Contract summary
- Approve / Reject buttons

Use realistic but fictional data. No real company names or private data.
Implementation target: Next.js + Tailwind + shadcn/ui + React Flow.
```

---

## 3. New-Node Prompt

```
Add a new OrchestAI node:

Type: <category>.<kebab-name>
Display name: <…>
Description: <…>
Inputs: list of { key, type (DataType), required, default? }
Outputs: list of { key, type (DataType) }
Configuration: list of { key, type, required, default?, allowedValues? }
Logic: <plain-English explanation of what the node does>

Requirements:
- Implement IWorkflowNodeDescriptor and IWorkflowNode.
- Use DI (no static singletons).
- Persist nothing to workflow execution tables.
- Add the node to its category's Add{Category}Nodes() extension.
- Add unit tests (happy path + a failure path).
- Update docs/NODES.md.
- (Optional) Add a sample workflow demonstrating the node.

Do not modify the engine.
```

---

## 4. New-Workflow-Feature Prompt

```
Implement a workflow feature: <describe feature>.

Constraints:
- Workflows remain data. Do not hardcode logic in the application layer.
- If you need a new construct, prefer:
  1. A new node (preferred).
  2. A new edge attribute (e.g. condition variant) — only if it’s a graph primitive.
  3. An engine change — only if it’s a primitive the SDK can’t express.
- Add engine validation for any new construct.
- Update workflow JSON schema and validators.
- Add tests.
- Update docs/WORKFLOW-ENGINE.md and docs/ARCHITECTURE.md as needed.
```

---

## 5. New-AI-Capability Prompt

```
Add an AI capability: <name>.

Constraints:
- Use ILLMProvider (no direct provider SDK calls in nodes).
- Prefer GenerateStructuredAsync<T> with a JSON Schema.
- Define a versioned prompt template under services/OrchestAI.AI/Prompts/.
- Record usage (provider, model, prompt_version, tokens, cost).
- Add tests with a FakeLLMProvider.
- Document the prompt and its expected schema.
```

---

## 6. Documentation Pass Prompt

```
Update OrchestAI documentation for the change described.

Touch:
- docs/<relevant doc> for the concept.
- README.md if it’s user-facing.
- docs/NODES.md if a node was added/changed.
- docs/API.md if an endpoint was added/changed.
- ADR in docs/adr/ if it was a non-trivial architectural decision.

Rules:
- Examples must be runnable as-is.
- Keep terminology aligned with docs/GLOSSARY.md.
- Update diagrams when behavior changes.
```
