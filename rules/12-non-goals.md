# Rules: Non-Goals (MVP Scope Guard)

The following **must not** be built in MVP (Phases 0–8) without explicit scope approval and a Product Architect decision.

Attempting to implement any of these is scope creep. Stop, flag it, and propose a roadmap phase for it instead.

---

## Deferred to Post-MVP

| Feature | Earliest Phase |
|---------|---------------|
| Full plugin/node marketplace | Phase 16 |
| Complex multi-agent orchestration | Phase 13 |
| Full RAG platform (knowledge bases, embeddings) | Phase 12 |
| Dozens of integrations (Salesforce, SharePoint, etc.) | Phase 16 |
| Kubernetes deployment / Helm charts | Phase 15+ |
| Enterprise SSO (SAML / OIDC) | Phase 14 |
| Billing and metering | Phase 15+ |
| Advanced analytics dashboards | Phase 15 |
| AI-generated workflows from natural language | Phase 17 |
| Public plugin ecosystem / SDK distribution | Phase 16 |
| Parallel node execution (`logic.parallel`) | Phase 10 |
| Loop nodes (`logic.loop`) | Phase 10 |
| Switch nodes (`logic.switch`) | Phase 10 |
| Subworkflow invocation | Phase 10+ |
| Error boundaries / compensation | Phase 11 |
| Long-running durable timers | Phase 10 |
| Field-level database encryption | Phase 14 |
| Secrets vault integration | Phase 14 |
| Per-tenant network egress allowlists | Phase 14 |
| SIEM / audit log export | Phase 14 |
| OCR node | Phase 11 |
| Document classification node | Phase 11 |
| Multi-provider AI fallback (circuit breaker) | Phase 11 |

---

## Why This Matters

The goal of MVP is to **prove the architecture**, not to ship every feature:

- Workflow as data ✓
- Nodes as reusable components ✓
- Engine-driven execution ✓
- AI as structured workflow steps ✓
- Human approval ✓
- Full execution traceability ✓

Once that foundation works, the platform expands naturally. A bloated MVP risks never shipping.
