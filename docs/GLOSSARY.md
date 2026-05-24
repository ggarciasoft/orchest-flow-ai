# Glossary

A shared vocabulary for OrchestFlowAI. If you find a term used inconsistently, fix it here and in the relevant doc.

| Term                          | Meaning                                                                                                  |
| ----------------------------- | -------------------------------------------------------------------------------------------------------- |
| **Workflow**                  | A named, versioned graph of nodes and edges representing a business process.                              |
| **Workflow Version**          | An immutable snapshot of a workflow definition. Only one version per workflow is active at a time.        |
| **Workflow Definition**       | The JSON document describing nodes, edges, configuration, and inputs/outputs.                             |
| **Workflow Execution**        | A single run of a workflow with concrete inputs. Has its own lifecycle (Queued → Running → …).            |
| **Node**                      | A reusable, stateless component that performs work — AI call, document operation, condition, approval, … |
| **Node Type**                 | Globally unique id like `ai.contract-risk-analysis`.                                                      |
| **Node Descriptor**           | Static metadata describing a node (display name, inputs, outputs, configuration schema).                  |
| **Node Execution**            | A single execution of a node within a workflow execution.                                                 |
| **Edge**                      | A directed connection between two nodes. May carry a boolean condition and an input mapping.              |
| **Input / Output (Port)**     | Typed data ports declared by a node descriptor.                                                           |
| **Configuration**             | Per-instance settings declared in the workflow JSON for a node.                                           |
| **Engine**                    | The runtime that loads, validates, and executes workflow definitions.                                     |
| **Worker**                    | The background service that picks up executions and drives the engine.                                    |
| **AI Runtime**                | The provider-agnostic LLM layer (prompts, structured outputs, usage tracking).                            |
| **Provider**                  | An LLM backend (OpenAI, Azure OpenAI, Anthropic, local, …).                                              |
| **Approval Request**          | A pending human decision attached to a paused node execution.                                             |
| **Tenant**                    | An isolated organization in the multi-tenant system. All data is scoped to a tenant.                      |
| **Audit Log**                 | Append-only record of security-relevant events.                                                           |
| **AI Usage Log**              | A row recording provider, model, tokens, and cost for one LLM call.                                       |
| **Correlation ID**            | An id that ties together logs/traces/metrics across services for a single logical action.                 |
| **Prompt Version**            | Versioned identifier of a prompt template; stored with every LLM call.                                    |
| **Document Reference (`DocumentRef`)** | A typed pointer to an uploaded document: `{ documentId, mimeType, sizeBytes }`.                   |
| **Structured Output**         | LLM output validated against a JSON schema; preferred for business decisions.                             |
| **Human-in-the-Loop (HITL)**  | Workflow pattern in which a human review/approval is required to proceed.                                 |
| **Idempotency Key**           | Optional client-provided key on mutating requests to prevent duplicates.                                  |
| **Pause / Resume**            | Engine ability to suspend a workflow waiting for an external signal (approval, timer) and continue later. |
| **Composition Root**          | The service (Api/Worker) that wires DI; thin and free of business logic.                                  |
