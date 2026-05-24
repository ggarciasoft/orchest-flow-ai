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

8a. The public documentation site (`/docs`) mirrors `docs/*.md`. When a new `.md` file is added to `docs/`, a corresponding entry **must** be added to `apps/web/src/content/docs/index.ts` in the same PR. The entry must have the correct `slug`, `title`, `category`, and `filename`. Failing to do this leaves the docs site stale.

8. The `README.md` is the entry point for new contributors. It must always:
   - Explain what OrchestAI is.
   - Link to the documentation map.
   - Include the repo layout.
   - Show the MVP demo workflow.

9. Changelog (`CHANGELOG.md`) is maintained for user-facing changes. Entries follow Keep a Changelog format.

10. No documentation for features that don't exist yet (except clearly marked roadmap sections in `docs/ROADMAP.md`).

---

## Code Comments — Mandatory Standards

11. **Every public C# method, class, and interface must have XML documentation comments:**
    ```csharp
    /// <summary>
    /// Executes the workflow graph starting from the system.start node.
    /// </summary>
    /// <param name="definition">The parsed workflow definition containing nodes and edges.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The completed or faulted execution result.</returns>
    public Task<ExecutionResult> ExecuteAsync(WorkflowDefinition definition, CancellationToken ct)
    ```
    - `<summary>` — what it does (not how)
    - `<param>` — for every non-obvious parameter
    - `<returns>` — what is returned and when it may be null
    - `<exception>` — any exceptions intentionally thrown

12. **Every ASP.NET controller action must have XML docs + HTTP metadata comments:**
    ```csharp
    /// <summary>Creates a new workflow and its initial empty version.</summary>
    /// <param name="req">Workflow name, description, and initial definition.</param>
    /// <response code="201">Workflow created successfully.</response>
    /// <response code="400">Validation error in request body.</response>
    [HttpPost]
    [ProducesResponseType(typeof(WorkflowResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<WorkflowResponse>> Create(...)
    ```

13. **Complex logic blocks must have inline comments explaining WHY, not WHAT:**
    ```csharp
    // Deactivate all existing versions before activating the new one
    // because only one version can be active per workflow at a time.
    foreach (var v in versions) v.Deactivate();
    ```
    - Do NOT comment obvious code: `// increment counter` above `i++`
    - DO comment business rules, non-obvious algorithms, workarounds, and tradeoffs

14. **Every TypeScript/React component must have a JSDoc comment:**
    ```tsx
    /**
     * WorkflowDesigner — full-screen canvas for building workflow graphs.
     * Handles node drag-and-drop, edge connections, config drawer, and execution.
     *
     * @param workflow - The workflow metadata (id, name, description)
     * @param nodeCatalog - Available node types fetched from /api/nodes
     */
    export function WorkflowDesigner({ workflow, nodeCatalog }: Props) {
    ```

15. **Every TypeScript lib function must have JSDoc:**
    ```ts
    /**
     * Formats an ISO date string for display in the UI.
     * Returns "—" if the value is null or undefined.
     */
    export function formatDate(iso: string | undefined): string
    ```

16. **Non-obvious React hooks, callbacks, and effects must have a one-line comment:**
    ```tsx
    // Close context menu when clicking anywhere on the pane
    const onPaneClick = useCallback(() => setContextMenu(null), []);
    ```

17. **TODO/FIXME comments must include a tracking reference:**
    ```csharp
    // TODO(BACKLOG#2): Replace with real PostgreSQL repository once DB is wired up
    // FIXME: Token expiry is not handled — refresh logic needed
    ```
    Bare `// TODO` with no context is not acceptable.

18. **Comments are updated in the same PR as the code they describe.** Stale comments that contradict the code are worse than no comments.
