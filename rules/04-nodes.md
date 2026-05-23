# Rules: Node Authoring

1. Every node **must** have all of the following:
   - `Type` — globally unique, format `category.kebab-name`
   - `DisplayName` — human-readable label
   - `Description` — one-sentence explanation
   - `Category` — one of `ai | documents | logic | human | integrations | system`
   - `Version` — semver string
   - `Inputs` — typed, required flag, default where applicable
   - `Outputs` — typed
   - `Configuration` — typed, required flag, allowed values where applicable

2. Node type ids are **globally unique**. Before picking a type id, confirm it is not taken.

3. Node outputs must be JSON-serializable. No raw streams, file handles, or unserializable objects.

4. Use `NodeExecutionException` for explicit failures. Set `Code` (short error code) and `Retryable` (bool):
   - Transient errors (HTTP 5xx, rate limits, DB transient) → `Retryable = true`
   - Validation, auth, schema mismatch → `Retryable = false`

5. Nodes are stateless. They never write to `workflow_executions`, `node_executions`, or any execution table.

6. Nodes do not know about UI concerns (colors, icons are in descriptors; render logic is in the UI).

7. Nodes must be independently testable. The `TestContext` helper from the SDK provides a fake `WorkflowExecutionContext` without engine plumbing.

8. Never share mutable state across concurrent node executions.

9. Always respect the `CancellationToken` passed to `ExecuteAsync`.

10. Breaking changes to a node's public contract (input/output keys, types) require a new type id (e.g. `ai.summarize.v2`). Minor/patch changes are transparent to existing workflows.

11. Register every node via its category's `Add{Category}Nodes()` DI extension. Never register nodes directly in service composition roots.

12. Do not make irreversible external actions (send email, post webhook) without an upstream human approval node — unless explicitly configured by the user via node config.
