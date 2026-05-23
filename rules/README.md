# Rules Index

All rules are **non-negotiable** unless superseded by an ADR in `docs/adr/`.

Every agent reads all rule files relevant to their scope.

---

## Rule Files

| File | Domain |
|------|--------|
| [`01-general.md`](./01-general.md) | Core architectural principles (apply to everyone) |
| [`02-backend.md`](./02-backend.md) | .NET / C# backend rules |
| [`03-frontend.md`](./03-frontend.md) | Next.js / React / TypeScript rules |
| [`04-nodes.md`](./04-nodes.md) | Node authoring rules |
| [`05-ai-nodes.md`](./05-ai-nodes.md) | AI-specific rules |
| [`06-database.md`](./06-database.md) | PostgreSQL / schema / migrations |
| [`07-security.md`](./07-security.md) | Auth, tenancy, secrets, hardening |
| [`08-observability.md`](./08-observability.md) | Logging, tracing, metrics |
| [`09-testing.md`](./09-testing.md) | Test requirements |
| [`10-documentation.md`](./10-documentation.md) | Documentation standards |
| [`11-pr-hygiene.md`](./11-pr-hygiene.md) | PR and commit standards |
| [`12-non-goals.md`](./12-non-goals.md) | What NOT to build (MVP scope guard) |
