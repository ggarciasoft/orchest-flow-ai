# Rules: General (Core Architectural Principles)

These apply to **every agent, every layer, every PR**.

---

1. **Workflows are data.** Never hardcode workflow logic in application code. Workflows are stored as versioned JSON definitions.

2. **Workflows are graphs.** A workflow is a structured definition containing nodes, edges, inputs, outputs, configuration, version, and metadata — nothing more.

3. **The engine executes nodes through interfaces.** `IWorkflowNode` is the only contact point. The engine must not import specific node implementations.

4. **Nodes are reusable and stateless.** A node does not persist workflow execution state. The engine owns state.

5. **The engine owns state management.** All execution state transitions (`WorkflowExecution`, `NodeExecution`) are the engine's responsibility.

6. **All API endpoints filter by tenant.** No cross-tenant data access is possible at any layer.

7. **AI outputs for business decisions must be structured.** JSON schema validated responses via `GenerateStructuredAsync<T>`. Free-form text is only acceptable for display-only outputs.

8. **Avoid provider lock-in.** All LLM calls go through `ILLMProvider`. No direct provider SDK calls in nodes or application logic.

9. **Clean Architecture boundaries are enforced.**
   - `Domain` → zero infrastructure dependencies
   - `Application` → Domain + Contracts + abstractions only
   - `Infrastructure` → implements Application/Domain abstractions
   - `Services` (Api/Worker) → thin composition roots, no business logic

10. **Secrets never live in workflow JSON or logs.** Provider keys, connection strings, and tokens come from environment variables or vault only.

11. **Every code change that affects behavior, APIs, nodes, schema, or UI must update the relevant documentation in the same commit.** Documentation is not optional or deferred. Specific requirements:
    - New or changed node → `docs/NODES.md`
    - New or changed API endpoint / response shape → `docs/API.md`
    - Schema change → `docs/DATABASE.md`
    - New config field or environment variable → `docs/SETUP.md` + `.env.example`
    - UI / designer behavior change → `docs/FRONTEND.md`
    - Test count change → `README.md` badges + `BACKLOG.md`
    - A commit without matching doc updates is **incomplete**.
