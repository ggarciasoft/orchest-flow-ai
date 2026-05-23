# RULES.md — Architectural & Coding Rules

These rules are **non-negotiable** unless an ADR is added that explicitly supersedes them. Apply across all agents, all code, all PRs.

---

## 1. General

1. **Do not hardcode workflows** in application logic. Workflows are data.
2. **Workflows are JSON.** They are stored, versioned, validated, and executed as structured data.
3. **The engine executes nodes through interfaces.** It must not know specific node implementations.
4. **Nodes are reusable and isolated.** Nodes do not persist workflow execution state.
5. **The engine owns state management.**
6. **All API endpoints validate tenant scope.** No cross-tenant data access is possible.
7. **AI outputs used for business decisions must be structured** (JSON schema validated).
8. **Avoid provider lock-in for AI integrations.** Always go through the `ILLMProvider` abstraction.
9. **Prefer Clean Architecture boundaries.** Domain has no infrastructure dependencies.
10. **Never store secrets in workflow JSON or logs.**

---

## 2. Backend (.NET)

1. Keep domain entities free from infrastructure concerns.
2. Application layer handles use cases (commands/queries).
3. Infrastructure implements external dependencies behind interfaces defined upstream.
4. API controllers are thin — validate, dispatch, return.
5. Worker uses Application services; it does not duplicate business logic.
6. All async methods accept and respect `CancellationToken`.
7. All execution state changes must be persisted.
8. Failed node executions must store `error_message`.
9. Use idempotency for retried operations.
10. Do not store secrets in workflow JSON or log them.
11. Multi-tenant filtering happens at the repository layer (default behavior), not by remembering to add `WHERE` clauses.
12. Validate inputs with FluentValidation (commands) and engine validation (workflow definitions).
13. Use Serilog with structured fields; include correlation/execution ids.
14. Public APIs have XML docs.

---

## 3. Frontend

1. The workflow designer **renders nodes dynamically from `/api/nodes/catalog`.** Do not hardcode node forms when descriptors can drive them.
2. Keep workflow canvas state separate from the persisted definition; translate at save time.
3. Use a typed API client (generated from OpenAPI). No hand-rolled `fetch` for endpoints in the spec.
4. Show validation errors before saving workflows.
5. Execution views are read-only.
6. Approval actions require confirmation.
7. No `any` in TypeScript. Use generated types or local Zod schemas.
8. Every screen handles loading / empty / error states explicitly.
9. Use Tailwind + shadcn/ui primitives; avoid one-off CSS where a primitive exists.
10. No secrets in the browser bundle.

---

## 4. Node Development

1. Every node must have:
   - `type` (globally unique, `category.kebab-name`)
   - `displayName`, `description`, `category`, `version`
   - `inputs`, `outputs`, `configuration` (all typed)
2. Node types are globally unique.
3. Node outputs must be serializable (JSON-friendly).
4. Errors are explicit; use `NodeExecutionException` with `Code` and `Retryable`.
5. Nodes are stateless.
6. Nodes do not know about UI concerns.
7. Nodes are independently testable (no engine plumbing required).
8. Nodes do not persist workflow execution rows.

---

## 5. AI Nodes

1. AI nodes log provider and model on every call.
2. AI nodes track token usage (via the AI Runtime).
3. AI prompts are versioned (`promptVersion`).
4. AI nodes prefer structured output. Schema validation is mandatory for business decisions.
5. AI nodes expose confidence/risk when relevant.
6. AI nodes must not perform irreversible external actions without an upstream human approval, unless explicitly configured.
7. User-provided content is wrapped in delimited tags to mitigate prompt injection.

---

## 6. Database

1. Migrations are forward-only in production; data backfills are explicit scripts.
2. Every domain table includes `tenant_id` where applicable.
3. All queries filter by tenant via the repository layer.
4. Use `jsonb` for free-form payloads; avoid storing structured data only in JSON.
5. Don't add indexes speculatively — add them when access patterns are confirmed.

---

## 7. Security

1. Authentication required for all non-public endpoints.
2. Tenant resolution happens once per request from the auth context.
3. Role-based authorization is enforced in the Application layer, not just controllers.
4. HTTPS-only in production.
5. Rate limiting per tenant and per IP.
6. Redact secrets and sensitive patterns in logs.
7. CSP, HSTS, and SameSite cookies in production.

---

## 8. Observability

1. Every request carries a `CorrelationId`; it propagates to workers, AI calls, and logs.
2. All logs are structured. No bare strings without fields.
3. Required metrics for engine and AI calls are emitted from day one.
4. The execution timeline is the authoritative debugging surface; it must be complete.

---

## 9. Testing

1. Domain logic has unit tests.
2. Engine has integration tests using fake nodes and a fake LLM provider.
3. Critical user journeys have E2E tests (Playwright).
4. Node authors ship at least one happy-path and one failure-path test.
5. Snapshot prompt templates; assert structured outputs match expected shape.

---

## 10. Documentation

1. Every public surface (API endpoint, node, package) is documented before merge.
2. Architectural decisions get an ADR (`docs/adr/NNNN-title.md`).
3. README and docs reflect the code that exists. If you change behavior, update docs in the same PR.
4. Examples in docs must run as-is.

---

## 11. PR Hygiene

1. PRs include a description: what, why, how to verify.
2. PRs include tests when they change behavior.
3. PRs include doc updates when they change behavior.
4. PRs do not mix unrelated changes.
5. Avoid `// TODO` without an issue link.

---

## 12. Non-Goals (don't violate the MVP scope)

Do not implement features outside MVP without scope approval:

- Full plugin marketplace
- Multi-agent orchestration
- RAG platform
- Many integrations
- Kubernetes deployment
- SSO / SAML / OIDC
- Billing
- AI-generated workflows from natural language
