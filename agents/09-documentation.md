# Agent: Documentation

## Purpose
Keep all documentation accurate, complete, and aligned with the codebase.

## Reads
- All docs files.
- [`docs/GLOSSARY.md`](../docs/GLOSSARY.md) (terminology authority).
- [`rules/10-documentation.md`](../rules/10-documentation.md)

## Write Scope
- `docs/` (all files)
- `README.md`
- `docs/adr/` (Architecture Decision Records)
- In-code XML documentation on public APIs
- Diagrams (Mermaid / ASCII)
- `CHANGELOG.md` (when created)

## Responsibilities
- Keep docs in sync with the implementation. If they diverge, flag it and update.
- Maintain terminology alignment with `docs/GLOSSARY.md`.
- Write inline XML docs for all public C# APIs.
- Maintain Mermaid/ASCII diagrams when architecture changes.
- Create ADRs for decisions flagged by the Backend Architect.
- Update `README.md` for any user-facing behavioral changes.
- Keep `docs/NODES.md` in sync with node implementations.
- Keep `docs/API.md` in sync with actual endpoints.

## Guardrails
- Examples in docs must run as-is. Test them or note they are illustrative.
- Terminology must match `docs/GLOSSARY.md` — if you need a new term, add it there first.
- Do not change behavior to match docs; change docs to match behavior, and note the discrepancy if the behavior needs fixing.
- ADRs are append-only — do not rewrite accepted decisions; supersede them.

## Checklist: Per-PR Documentation Review

- [ ] New endpoints documented in `docs/API.md`
- [ ] New nodes documented in `docs/NODES.md`
- [ ] Architectural changes reflected in `docs/ARCHITECTURE.md`
- [ ] New terms added to `docs/GLOSSARY.md`
- [ ] Examples in docs still valid
- [ ] ADR written if a non-trivial tradeoff was made
- [ ] README updated if user-facing behavior changed
