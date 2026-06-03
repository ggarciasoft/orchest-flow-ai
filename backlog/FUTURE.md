# OrchestFlowAI — Future Backlog

> Planned items and ideas, grouped by effort and value.

---

## 🌟 Top Picks (recommended starting points)

> Given real-world usage patterns (Gmail sync, external data intake, forms):
>
> 1. **Error explainer** — immediate day-to-day value, tiny to build
> 2. **AI Decision node** — simplifies routing logic significantly
> 3. **Agent nodes** — the long-term differentiator; makes OrchestFlowAI a true AI orchestrator rather than a workflow tool with AI nodes

---

## 🟢 Low effort, high value

| Item | Notes |
|---|---|
| **AI Decision / Router node** | Natural-language condition: *"Route to 'high-value' if amount > 1000 OR customer is premium"* — LLM interprets the text and returns a branch label. Replaces complex condition chains with plain English. |
| **AI-generated form summaries** | After a form is submitted, run a quick LLM pass to produce a one-line summary of what the user entered. Shows up in the approval inbox and execution timeline instead of raw key-value pairs. |
| **Execution error explainer** | When a node fails, add an "Explain this error" button. Sends the error message + node config to the LLM and gets back a plain-English explanation + suggested fix. |
| **AI-suggested node config** | In the config drawer, an "Auto-fill with AI" button: describe what you want the node to do, and the LLM fills in the config fields. |

## 🟡 Medium effort

| Item | Notes |
|---|---|
| **AI workflow analyzer** | `POST /api/workflows/{id}/analyze` — LLM reviews the workflow definition and returns: potential bottlenecks, missing error handling, suggested improvements, documentation. |
| **Conversational execution monitor** | Chat with a running execution: *"Why is this workflow stuck?"* — agent reads the timeline, node outputs, and approval state to answer. |
| **AI form field extraction from documents** | Upload PDF/image → AI extracts structured data → auto-fills a form or creates a form definition from the document's fields. |
| **Smart retry with AI** | On node failure, before retrying, the LLM can transform the inputs or adjust config based on the error — e.g. truncate a prompt that was too long, fix a malformed JSON. |
| **Partial AI workflow/form patches** | Instead of returning a complete definition, the LLM returns only the diff/patch (new nodes, changed configs). The backend merges the partial result into the existing definition. Same for forms. Avoids token-limit truncation and makes incremental edits natural. |

## 🔴 Larger, more architectural

| Item | Notes |
|---|---|
| **Agent nodes — multi-step LLM reasoning** | A node that runs a mini agent loop: give it a goal and available tools (other nodes as callable functions), and it plans + executes steps autonomously. ReAct pattern inside a workflow node. |
| **Workflow generation from examples** | Show the LLM 2-3 input/output examples and have it design a workflow that would produce those outputs from those inputs. |
| **Semantic search over executions** | Index execution inputs/outputs in a vector store. Query: *"Find all executions where the customer was from Bolivia"* — returns relevant runs without exact string matching. |
| **AI-driven cron scheduling** | *"Run this every weekday morning when my team is online"* → LLM translates to a cron expression with timezone. |

## Infrastructure / triggers

| Item | Notes |
|---|---|
| API Key Trigger | `POST /api/workflows/{id}/run?apiKey=***` — no JWT; for external systems |
| Call Workflow node | `system.call-workflow` — start a child workflow inline, optionally wait for completion |
| Re-run with Modified Inputs | Fork an existing execution with optional input overrides |
| Email Trigger | IMAP polling or webhook from SendGrid/Mailgun starts a workflow |
| File Drop Trigger | Watched folder or S3/GCS/Azure Blob starts a workflow |
| Slack Command Trigger | Slash command or @mention starts a workflow |
| Bulk / CSV Run | Upload CSV → one execution per row |
| Multi-tenant admin panel | Super-admin view across tenants |
| Dark mode | Tailwind `dark:` classes not yet wired |
| Export/import workflow definitions | Download/upload workflow JSON |
| Webhook node inbound signature verification | HMAC validation on inbound webhooks |
| Ollama model list dynamic | Surface `llm.ollama.models` setting in UI |
