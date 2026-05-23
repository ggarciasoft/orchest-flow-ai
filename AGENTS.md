# AGENTS.md

> **This file is an index. Full agent definitions have been split into individual files.**

See [`agents/`](./agents/) for the complete agent roster.

---

## Quick Reference

| Agent | File | Focus |
|-------|------|-------|
| Product Architect | [`agents/01-product-architect.md`](./agents/01-product-architect.md) | Vision, scope, roadmap |
| Backend Architect | [`agents/02-backend-architect.md`](./agents/02-backend-architect.md) | Architecture, boundaries, ADRs |
| Workflow Engine | [`agents/03-workflow-engine.md`](./agents/03-workflow-engine.md) | Runtime, state, retries, pause/resume |
| Node SDK | [`agents/04-node-sdk.md`](./agents/04-node-sdk.md) | Node interfaces, registry, testing helpers |
| AI Runtime | [`agents/05-ai-runtime.md`](./agents/05-ai-runtime.md) | LLM abstraction, structured outputs, prompts |
| Frontend | [`agents/06-frontend.md`](./agents/06-frontend.md) | Web app, designer, screens |
| DevOps | [`agents/07-devops.md`](./agents/07-devops.md) | Docker, CI/CD, migrations, observability |
| QA | [`agents/08-qa.md`](./agents/08-qa.md) | Unit, integration, E2E tests |
| Documentation | [`agents/09-documentation.md`](./agents/09-documentation.md) | Docs, diagrams, ADRs, changelog |
| Orchestrator | [`agents/10-orchestrator.md`](./agents/10-orchestrator.md) | Task routing, multi-agent coordination |

## Rules

All agents are bound by [`rules/`](./rules/).


## Unit Test Enforcement (Non-Negotiable)

> See full rules in `rules/09-testing.md` sections 14-20.

**Every agent that writes production code must:**
1. Run the relevant test suite before AND after changes.
2. Create unit tests for every new class, component, or function introduced.
3. Update unit tests broken by the change (never delete or skip them silently).
4. Commit code and tests together in the same PR/commit.
5. Never mark a task as done if tests are failing.

| Layer | Test command | Test location |
|---|---|---|
| Backend (.NET) | `dotnet test` | `tests/OrchestAI.Tests/` |
| Frontend | `npm test` | `src/**/__tests__/` |
