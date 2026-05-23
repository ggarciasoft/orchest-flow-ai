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

### `ai.classify`  · Phase 11

- **Purpose:** Classify text into a configurable set of labels.

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

### `ai.translate`  · Phase 11

- **Purpose:** Translate text between languages.

### `ai.policy-check`  · Phase 11

- **Purpose:** Check text against a policy spec; return compliant/violations.

---

## Logic

### `logic.condition`  · MVP

- **Purpose:** Evaluate a boolean expression and route to the matching branch.
- **Inputs:** all upstream outputs are available in the expression scope
- **Outputs:** `result` (`Boolean`)
- **Config:** `expression` (`String`, required)

### `logic.switch`  · Phase 10

- **Purpose:** Multi-way routing by a key value.

### `logic.loop`  · Phase 10

- **Purpose:** Iterate over an array with bounded iterations.

### `logic.parallel`  · Phase 10

- **Purpose:** Fan out to N branches; join when all complete.

### `logic.delay`  · Phase 10

- **Purpose:** Pause the workflow for a configured duration.

### `logic.retry-policy`  · Phase 10

- **Purpose:** Override default retry behavior for a downstream node.

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

## Integrations  · Phase 11+

### `integration.email.send`

- **Purpose:** Send an email via configured provider.

### `integration.webhook.call`

- **Purpose:** POST a payload to an external webhook.

### `integration.http.request`

- **Purpose:** Arbitrary HTTP request (GET/POST/PUT/PATCH/DELETE) with auth.

### `integration.slack.send-message`

### `integration.teams.send-message`

### `integration.jira.create-ticket`

### `integration.database.query`

### `integration.salesforce.create-record`  · Phase 16

### `integration.sharepoint.upload-file`  · Phase 16

---

## Priorities for Post-MVP

| Tier   | Nodes                                                                                  |
| ------ | -------------------------------------------------------------------------------------- |
| High   | `integration.email.send`, `integration.http.request`, `logic.switch`, `ai.summarize`, `ai.classify`, `ai.extract-entities`, `document.generate-pdf-report`, `integration.webhook.call`, `ai.rag-search`, `human.manual-review` |
| Medium | `integration.slack.send-message`, `integration.jira.create-ticket`, `document.ocr`, `logic.parallel`, `logic.delay`, `ai.translate`, `ai.compare-documents`, `integration.database.query` |
| Advanced | `ai.agent-executor`, `ai.multi-agent-review`, `ai.workflow-planner`, `system.error-boundary`, `system.compensation-step` |
