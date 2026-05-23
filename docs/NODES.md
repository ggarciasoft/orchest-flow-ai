# Node Catalog

This is the canonical list of node types in OrchestAI. **MVP** nodes ship in the first release; the rest are roadmap.

Each entry has:
- `type` — unique id
- **Purpose**
- **Inputs**, **Outputs**, **Config**
- **Status** — MVP / Phase N

---

## System

### `system.start`  · MVP

- **Purpose:** Entry point of every workflow. Surfaces workflow inputs.
- **Inputs:** none
- **Outputs:** all workflow inputs as named outputs
- **Config:** none

### `system.end`  · MVP

- **Purpose:** Terminal node. Marks workflow completion.
- **Inputs:** any final values to persist as workflow output
- **Outputs:** none
- **Config:** none

### `system.error-boundary`  · Phase 11

- **Purpose:** Catch errors from a sub-graph and route to handler nodes.

### `system.compensation-step`  · Phase 11

- **Purpose:** Define rollback behavior for a node.

---

## Documents

### `document.extract-pdf-text`  · MVP

- **Purpose:** Extract plain text from a PDF document.
- **Inputs:** `document` (`DocumentRef`, required)
- **Outputs:** `text` (`String`), `pageCount` (`Number`)
- **Config:** `ocrFallback` (`Boolean`, default `false`)

### `document.ocr`  · Phase 11

- **Purpose:** OCR scanned documents/images.

### `document.classify`  · Phase 11

- **Purpose:** Classify document by type (invoice, contract, NDA, …).

### `document.split`  · Phase 11

- **Purpose:** Split multi-document PDFs by detected boundaries.

### `document.extract-tables`  · Phase 11

- **Purpose:** Extract structured tables from documents.

### `document.generate-pdf-report`  · Phase 11

- **Purpose:** Render a templated PDF report from data.

---

## AI

### `ai.contract-risk-analysis`  · MVP

- **Purpose:** Analyze a contract and return structured risk assessment.
- **Inputs:** `text` (`String`, required)
- **Outputs:**
  ```json
  {
    "riskLevel": "Low|Medium|High",
    "summary": "string",
    "keyClauses": [{ "title": "string", "risk": "string", "reason": "string" }],
    "recommendedAction": "string"
  }
  ```
- **Config:** `model` (default), `riskThreshold` (enum: low|medium|high), `language` (default `auto`)

### `ai.executive-summary`  · MVP

- **Purpose:** Generate a 1–2 paragraph executive summary.
- **Inputs:** `text` (`String`, required)
- **Outputs:** `summary` (`String`)
- **Config:** `model`, `maxWords` (default 250), `tone` (formal|neutral|friendly)

### `ai.summarize`  · Phase 11

- **Purpose:** Generic text summarization with configurable length/style.

### `ai.classify`  ✅ Shipped

- **Purpose:** Classify text into configured categories using an LLM.
- **Inputs:** `text` (`String`, required)
- **Outputs:** `category` (`String`), `confidence` (`String`), `rawResponse` (`String`)
- **Config:** `categories` (comma-separated, required), `model`, `instructions`

### `ai.extract`  ✅ Shipped

- **Purpose:** Extract structured fields from unstructured text using an LLM.
- **Inputs:** `text` (`String`, required)
- **Outputs:** `extractedJson` (`Json`), plus individual field outputs
- **Config:** `fields` (comma-separated, required), `model`

### `ai.extract-entities`  · Phase 11

- **Purpose:** Extract named entities (people, orgs, dates, money, …).

### `ai.sentiment-analysis`  · Phase 11

- **Purpose:** Score sentiment / emotion of input text.

### `ai.compare-documents`  · Phase 11

- **Purpose:** Diff two documents semantically and highlight differences.

### `ai.generate-email`  · Phase 11

- **Purpose:** Compose an email from structured inputs and tone settings.

### `ai.agent-executor`  · Phase 13

- **Purpose:** Execute an agent loop with tools, memory, and stop conditions.

### `ai.rag-search`  · Phase 12

- **Purpose:** Search a knowledge base and return cited passages.

### `ai.translate`  ✅ Shipped

- **Purpose:** Translate text into a configured target language using an LLM.
- **Inputs:** `text` (`String`, required)
- **Outputs:** `translatedText` (`String`), `targetLanguage` (`String`)
- **Config:** `targetLanguage` (required), `model`

### `ai.policy-check`  · Phase 11

- **Purpose:** Check text against a policy spec; return compliant/violations.

---

## Logic

### `logic.condition`  · MVP

- **Purpose:** Evaluate a boolean expression and route to the matching branch.
- **Inputs:** all upstream outputs are available in the expression scope
- **Outputs:** `result` (`Boolean`)
- **Config:** `expression` (`String`, required)

### `logic.switch`  ✅ Shipped

- **Purpose:** Multi-way routing by a key value matched against configured cases.
- **Inputs:** `value` (`String`, required)
- **Outputs:** `matchedCase` (`String`), `matched` (`Boolean`)
- **Config:** `cases` (comma-separated, required)

### `logic.loop`  · Phase 10

- **Purpose:** Iterate over an array with bounded iterations.

### `logic.parallel`  · Phase 10

- **Purpose:** Fan out to N branches; join when all complete.

### `logic.delay`  ✅ Shipped

- **Purpose:** Pause the workflow for a configured duration.
- **Inputs:** none
- **Outputs:** `delayedMs` (`Number`)
- **Config:** `durationMs` (`Number`, required, default 1000)

### `logic.retry-policy`  · Phase 10

- **Purpose:** Override default retry behavior for a downstream node.

### `logic.merge`  ✅ Shipped

- **Purpose:** Synchronization point that collects outputs from multiple upstream branches and forwards all as outputs.
- **Inputs:** dynamic
- **Outputs:** all inputs forwarded
- **Config:** none

---

## Data  ✅ Shipped

### `data.set`  ✅ Shipped

- **Purpose:** Sets workflow variables with optional `{{placeholder}}` substitution from node inputs.
- **Inputs:** none
- **Outputs:** dynamic — all keys from the `variables` config
- **Config:** `variables` (JSON key→value map, required)

### `data.json-transform`  ✅ Shipped

- **Purpose:** Maps/reshapes a JSON object using dot-notation path mappings.
- **Inputs:** `json` (`String`, required)
- **Outputs:** `transformedJson` (`Json`) + individual mapped fields
- **Config:** `mapping` (JSON output→input path map, required)

---

## Human

### `human.approval`  · MVP

- **Purpose:** Pause workflow and request a human approve/reject decision.
- **Inputs:** `payload` (`Json`) — context shown to the approver
- **Outputs:** `decision` (`Enum: approved|rejected`), `comment` (`String`), `decidedBy` (`String`), `decidedAt` (`String`)
- **Config:**
  - `requiredWhen` (expression; if false, node is auto-skipped)
  - `assignees` (`Json`: users or roles)
  - `slaMinutes` (`Number`, optional)
  - `title` (`String`)

### `human.manual-review`  · Phase 11

- **Purpose:** Like approval, but the user edits / annotates a payload before continuing.

### `human.assign-task`  · Phase 11

- **Purpose:** Create a generic human task; outputs include completion data.

---

## Integrations  ✅ Partial — 4 nodes shipped

### `integrations.http`  ✅ Shipped

- **Purpose:** Calls any external REST API endpoint.
- **Inputs:** none
- **Outputs:** `statusCode` (`Number`), `responseBody` (`String`), `success` (`Boolean`)
- **Config:** `url` (required), `method` (GET/POST/PUT/DELETE/PATCH), `headers` (JSON), `body`, `timeoutSeconds`

### `integrations.slack`  ✅ Shipped

- **Purpose:** Sends a message to Slack via an incoming webhook URL.
- **Outputs:** `sent` (`Boolean`)
- **Config:** `webhookUrl` (required), `message` (supports `{{placeholders}}`), `channel`

### `integrations.webhook-out`  ✅ Shipped

- **Purpose:** POSTs execution context and optional node inputs to a configured URL.
- **Outputs:** `statusCode` (`Number`), `sent` (`Boolean`)
- **Config:** `url` (required), `includeInputs` (all|none)

### `integrations.email`  ✅ Shipped

- **Purpose:** Sends an email via SMTP. Subject and body support `{{placeholder}}` substitution.
- **Outputs:** `sent` (`Boolean`), `to` (`String`)
- **Config:** `to`, `subject`, `body` (all required + `{{placeholders}}`), `smtpHost`, `smtpPort`

### `integration.teams.send-message`  · Phase 12+

### `integration.jira.create-ticket`

### `integration.database.query`

### `integration.salesforce.create-record`  · Phase 16

### `integration.sharepoint.upload-file`  · Phase 16

---

## Node Inventory (as of 2026-05-23)

| Status | Count | Nodes |
|---|---|---|
| ✅ Shipped | 19 | `system.start`, `system.end`, `logic.condition`, `logic.delay`, `logic.switch`, `logic.merge`, `human.approval`, `ai.contract-risk-analysis`, `ai.executive-summary`, `ai.classify`, `ai.extract`, `ai.translate`, `documents.extract-pdf`, `integrations.http`, `integrations.slack`, `integrations.webhook-out`, `integrations.email`, `data.set`, `data.json-transform` |
| 🚧 Roadmap | 20+ | See Phase 10–16 items above |

## Priorities for Post-MVP (Remaining)

| Tier | Nodes |
| ------ | -------------------------------------------------------------------------------------- |
| High | `ai.summarize`, `document.generate-pdf-report`, `ai.rag-search`, `human.manual-review`, `logic.loop`, `logic.parallel` |
| Medium | `integration.jira.create-ticket`, `document.ocr`, `ai.compare-documents`, `integration.database.query`, `ai.sentiment-analysis` |
| Advanced | `ai.agent-executor`, `ai.multi-agent-review`, `ai.workflow-planner`, `system.error-boundary`, `system.compensation-step` |
