# Rules: Documentation

1. Every public surface is documented **before or in the same PR** as the code that introduces it:
   - New API endpoints → `docs/API.md`
   - New nodes → `docs/NODES.md`
   - New packages → package README or relevant `docs/` file
   - Public C# APIs → XML documentation comments

2. Architectural decisions that involve non-trivial tradeoffs get an ADR in `docs/adr/NNNN-title.md`. "Non-trivial" means: affects multiple teams, has long-term implications, or was debated.

3. Terminology must align with `docs/GLOSSARY.md`. If you use a new term, add it to the glossary first.

4. Examples in documentation must be runnable as-is, or must be clearly marked as illustrative with a note.

5. Documentation must reflect the code that exists, not aspirational code. If behavior diverges from docs, update docs in the same PR — or flag it as a separate issue.

6. ADRs are append-only. To change a decision, create a new ADR with `Status: Supersedes NNNN`.

7. Diagrams (Mermaid, ASCII) are updated when architecture or flow changes. Stale diagrams are worse than no diagrams.

8. The `README.md` is the entry point for new contributors. It must always:
   - Explain what OrchestAI is.
   - Link to the documentation map.
   - Include the repo layout.
   - Show the MVP demo workflow.

9. Changelog (`CHANGELOG.md`) is maintained for user-facing changes. Entries follow Keep a Changelog format.

10. No documentation for features that don't exist yet (except clearly marked roadmap sections in `docs/ROADMAP.md`).
