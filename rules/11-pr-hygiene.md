# Rules: PR Hygiene

1. PRs include a description: **what** changed, **why**, and **how to verify**.

2. PRs include tests when they change behavior. A behavior change without tests is incomplete.

3. **PRs include doc updates when they change anything user-facing or developer-facing.** This is non-negotiable:
   - New node → add to `docs/NODES.md`
   - New API endpoint or changed response → update `docs/API.md`
   - Schema change → update `docs/DATABASE.md`
   - New config field or env var → update `docs/SETUP.md` and `.env.example`
   - New category, UI component, or designer behavior → update `docs/FRONTEND.md`
   - Test count changes → update `README.md` badges and `BACKLOG.md`
   - A code change without the matching doc update is considered **incomplete**.

4. PRs do not mix unrelated changes. One concern per PR. Refactors and features are separate.

5. Avoid `// TODO` comments without a linked issue. If it's worth doing, it's worth tracking.

6. Conventional Commits are encouraged:
   - `feat:` — new feature
   - `fix:` — bug fix
   - `docs:` — documentation only
   - `chore:` — maintenance (deps, config, tooling)
   - `refactor:` — code restructure without behavior change
   - `test:` — tests only
   - `perf:` — performance improvement

7. Commits should be small and focused. Squash on merge if requested by the reviewer.

8. Branch naming:
   - `feature/<short-desc>`
   - `fix/<short-desc>`
   - `docs/<short-desc>`
   - `chore/<short-desc>`

9. PRs targeting `main` require at least one review before merge (when team size allows).

10. A failing CI check blocks merge. Do not merge a PR with a red CI unless the failure is confirmed as infrastructure noise and documented as such.

## PR Checklist

- [ ] Code follows all applicable rules in `rules/`
- [ ] Tests added or updated
- [ ] Docs added or updated
- [ ] No secrets or PII in code, logs, or test fixtures
- [ ] Migrations included if schema changed
- [ ] Designer/backend changes are consistent (descriptors + UI)
- [ ] Breaking changes documented with migration notes
- [ ] CI passing
