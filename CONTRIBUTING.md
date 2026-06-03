# Contributing

Thanks for your interest in OrchestFlowAI. This guide gets you from clone to merged PR.

---

## 1. Setup

See [`docs/SETUP.md`](./docs/SETUP.md).

---

## 2. Branching & Commits

- Branch from `main`. Use `feature/<short-desc>`, `fix/<short-desc>`, `docs/<short-desc>`.
- Conventional Commits encouraged: `feat:`, `fix:`, `docs:`, `chore:`, `refactor:`, `test:`.
- Keep commits focused; squash on merge if requested.

---

## 3. Coding Standards

Read [`RULES.md`](./RULES.md). The short version:

- **Backend:** Clean Architecture; cancellation tokens; structured logs; tenant filters at repo layer.
- **Frontend:** TypeScript strict; no `any`; render forms from descriptors; typed API client.
- **Nodes:** stateless; descriptors required; structured outputs for AI.
- **Tests:** ship them with code.
- **Docs:** update in the same PR as behavior changes.

---

## 4. Adding a New Node

Follow the skill in [`docs/NODE-SDK.md`](./docs/NODE-SDK.md) §10:

1. Pick `Type` id (`category.kebab-name`).
2. Implement descriptor.
3. Define inputs/outputs/configuration.
4. Implement `IWorkflowNode.ExecuteAsync`.
5. Register via the category's DI extension.
6. Tests: happy path + at least one failure.
7. Update [`docs/NODES.md`](./docs/NODES.md).
8. (Optional) Add a sample workflow under `samples/`.

---

## 5. Adding a New Workflow Feature

1. Decide layer: engine, node, API, or UI.
2. Avoid hardcoding workflow logic. Prefer node + descriptor changes.
3. Update workflow schema if needed (and migration if schema is versioned).
4. Add validation rules to the engine if new constructs are introduced.
5. Tests.
6. Docs.

---

## 6. Adding AI Capability

1. Use the `ILLMProvider` abstraction; don't call providers directly.
2. Prefer structured output (`GenerateStructuredAsync<T>`).
3. Add a versioned prompt template under `services/OrchestFlowAI.AI/Prompts/`.
4. Track usage (the runtime helper does this for you).
5. Store model/provider/prompt-version metadata on the node execution.
6. Add tests with a fake provider.

---

## 7. Adding a UI Screen

1. Define user goal and data needed.
2. Add a route under `app/(app)/…`.
3. Use shadcn/ui primitives.
4. Use TanStack Query for data; handle loading / empty / error states.
5. Connect to the typed API client.
6. Accessibility: keyboard nav, labels, focus management.

---

## 8. ADRs

For non-trivial architectural decisions, add an ADR:

```
docs/adr/NNNN-title.md
```

Use this template:

```md
# NNNN — Title

- **Status:** Proposed / Accepted / Superseded
- **Date:** YYYY-MM-DD
- **Deciders:** names

## Context

What problem are we solving?

## Decision

What did we decide?

## Consequences

Good, bad, and trade-offs.

## Alternatives

What else was considered, and why we passed.
```

---

## 9. Tests

Run before opening a PR:

```bash
# Backend (from repo root)
cd orchestr-flow-ai
dotnet test --configuration Release

# Frontend (from repo root — pnpm workspace filter)
pnpm --filter web test
```

CI will run the same.

---

## 10. PR Checklist

- [ ] Code follows [`RULES.md`](./RULES.md).
- [ ] Tests added/updated.
- [ ] Docs added/updated.
- [ ] No secrets or PII in code, logs, or fixtures.
- [ ] Migrations included (if schema changed).
- [ ] Designer/back-end change pairs are consistent (descriptors + UI).
- [ ] Backwards-compatible (or migration steps documented).

---

## 11. Reporting Bugs

Use the GitHub issue templates (`bug`, `feature`, `docs`).

For security issues, see [`docs/SECURITY.md`](./docs/SECURITY.md) §11. Do **not** open public issues.

---

## 12. Code of Conduct

Be respectful. Assume good intent. Default to clarity over cleverness. Disagree on substance, not on people.
