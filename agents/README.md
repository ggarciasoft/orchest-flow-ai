# Agents Index

This directory defines **AI development agent roles** for use with AI coding tools (Cursor, Antigravity, Claude Code, Codex, etc.).

Each file defines one agent: its purpose, docs to read, write scope, responsibilities, and guardrails.

**Cross-cutting rules always apply** → [`../rules/`](../rules/)

---

## Agent Roster

| File | Agent | Focus |
|------|-------|-------|
| [`01-product-architect.md`](./01-product-architect.md) | Product Architect | Vision, scope, roadmap |
| [`02-backend-architect.md`](./02-backend-architect.md) | Backend Architect | Architecture, boundaries, ADRs |
| [`03-workflow-engine.md`](./03-workflow-engine.md) | Workflow Engine | Runtime, state, retries, pause/resume |
| [`04-node-sdk.md`](./04-node-sdk.md) | Node SDK | Node interfaces, registry, testing helpers |
| [`05-ai-runtime.md`](./05-ai-runtime.md) | AI Runtime | LLM abstraction, structured outputs, prompts |
| [`06-frontend.md`](./06-frontend.md) | Frontend | Web app, designer, screens |
| [`07-devops.md`](./07-devops.md) | DevOps | Docker, CI/CD, migrations, observability scaffolding |
| [`08-qa.md`](./08-qa.md) | QA | Unit, integration, E2E tests |
| [`09-documentation.md`](./09-documentation.md) | Documentation | Docs, diagrams, ADRs, changelog |
| [`10-orchestrator.md`](./10-orchestrator.md) | Orchestrator | Task routing, multi-agent coordination |

---

## How to Use

1. A developer (or orchestrator agent) selects a role for the task.
2. The agent reads this index + the specific agent file + [`../rules/`](../rules/).
3. The agent works only within its declared **write scope**.
4. If a task crosses scopes, the agent stops and asks — or spawns a sibling agent.

## Defaults

| Task type | Default agent |
|-----------|---------------|
| Engine / state / persistence | Workflow Engine |
| Node author / catalog | Node SDK |
| LLM / prompts / structured outputs | AI Runtime |
| UI / designer / screens | Frontend |
| Docs / explanations | Documentation |
| Build / deploy / CI | DevOps |
| Tests | QA |
| Architecture / boundaries / ADRs | Backend Architect |
| Roadmap / scope | Product Architect |
| Multi-step routing | Orchestrator |
