# Agent: Node SDK

## Purpose
Own the developer experience for creating, registering, and testing workflow nodes.

## Reads
- [`docs/NODE-SDK.md`](../docs/NODE-SDK.md)
- [`docs/NODES.md`](../docs/NODES.md)
- [`rules/04-nodes.md`](../rules/04-nodes.md)

## Write Scope
- `packages/OrchestAI.SDK/`
- Node implementations under `nodes/`
- `docs/NODE-SDK.md`
- `docs/NODES.md`
- `samples/` (sample workflows demonstrating nodes)
- Node-level test projects

## Responsibilities
- Maintain `IWorkflowNode`, `IWorkflowNodeDescriptor`, and all port/config definition types.
- Ensure all nodes follow the descriptor contract (type, displayName, description, category, version, inputs, outputs, configuration).
- Provide `TestContext` and other SDK helpers so node authors don't need engine plumbing.
- Author and maintain MVP nodes:
  - `system.start`, `system.end`
  - `document.extract-pdf-text`
  - `ai.contract-risk-analysis`, `ai.executive-summary`
  - `logic.condition`
  - `human.approval`
- Register each node via the category's `Add{Category}Nodes()` DI extension.
- Keep `docs/NODES.md` up to date with every new node.

## Guardrails
- Do not implement engine logic.
- Do not couple the SDK to any specific provider (LLM, DB, etc.).
- Nodes are stateless — they never write to workflow execution tables.
- Breaking changes to a node's inputs/outputs/type require a new type id (e.g. `ai.summarize.v2`).

## How to Create a New Node (Skill)

1. Pick `type` id: `category.kebab-name`
2. Implement `IWorkflowNodeDescriptor` (all fields required)
3. Declare `Inputs`, `Outputs`, `Configuration` with `DataType` and `Required`
4. Implement `IWorkflowNode.ExecuteAsync` — pure logic, DI for providers
5. Register in `Add{Category}Nodes()` extension
6. Add unit tests: happy path + ≥1 failure path
7. Update `docs/NODES.md`
8. (Optional) Add a sample workflow under `samples/`

## DataType → UI Mapping

| DataType | UI Control |
|----------|-----------|
| String | text field / textarea |
| Number | number input |
| Boolean | toggle |
| Json | Monaco JSON editor |
| Binary | file picker |
| DocumentRef | document picker dialog |
| Enum | dropdown (AllowedValues) |
