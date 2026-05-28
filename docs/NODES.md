# Node Catalog

This is the canonical list of node types in OrchestFlowAI. **MVP** nodes ship in the first release; the rest are roadmap.

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

- **Purpose:** Extract structured fields from unstructured text using an LLM. Strips HTML tags and decodes entities before sending to the model. Uses a system prompt + `<input_text>` XML delimiters to prevent prompt injection.
- **Inputs:** `text` (`String`, required — or `item` / `body` as fallback; falls back to `NodeOutputs` scan when edge wiring is missing)
- **Outputs:** `extractedJson` (`Json`, the full JSON object); plus **individual field outputs** matching each key in `fields` (e.g. `Amount`, `Date`, `Currency`) for direct use as `{{Amount}}` in downstream nodes
- **Config:**

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `fields` | String | — | Comma-separated field names to extract, e.g. `Amount, Date, Store` |
| `formatPreset` | Enum | `none` | Built-in format rules. See presets below. |
| `formatInstructions` | String | — | Custom per-field rules appended after preset. E.g. `Amount: numeric only. Date: YYYY-MM-DD.` |
| `textInput` | Enum | `text` | Which upstream output key to use as input (`text`, `item`, `body`) |
| `model` | String | `default` | LLM model override |

**Format Presets:**

| Preset | Fields & Rules |
|--------|----------------|
| `none` | No preset rules — use `formatInstructions` for custom rules |
| `financial` | `Amount`: numeric only (e.g. `150.00`)<br>`Currency`: ISO 4217 code (`USD`, `DOP`, `EUR`, `GBP`…) detected from symbols (RD$ → DOP, $ → USD/DOP by context, € → EUR, £ → GBP)<br>`Date`: YYYY-MM-DD<br>`Category`: Food \| Transport \| Utilities \| Entertainment \| Healthcare \| Other<br>`Store`: merchant name only |
| `invoice` | `Amount`: numeric, `Date`: YYYY-MM-DD, `InvoiceNumber`: alphanumeric, `Vendor`: company name |
| `contact` | `Name`: title-case full name, `Email`: lowercase, `Phone`: E.164 (+1234567890), `Company`: name only |

**Notes:**
- `formatInstructions` appends after preset rules; use it to override specific fields or add extras.
- Individual field outputs (e.g. `{{Amount}}`, `{{Currency}}`) are always available — no need to parse `extractedJson`.
- Prompt injection protection: email body is wrapped in `<input_text>` tags; system prompt instructs the model to ignore any commands inside.

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

### `logic.foreach`  ✅ Shipped

**Category:** Logic
**Description:** Iterates over a JSON array. Supports two modes:

- **Fan-out mode (default, `loopMode=false`):** Expands the array into indexed outputs (`item_0..N`) for static wiring. Backward-compatible.
- **Loop mode (`loopMode=true`):** Emits `_foreach_items` so the engine executes downstream nodes **once per item** and collects results. Requires a `logic.foreach.end` node at the end of the loop body. After the loop, outputs `results` (JSON array of per-item outputs) and `count`.

**Input resolution:** `inputArray` key is canonical, but the node also accepts `emails`, `items`, `data`, `array`, `results` as aliases. When `NodeInputs` is empty (edge wiring didn't propagate), the node falls back to scanning `NodeOutputs` for any of those keys — making explicit edge wiring optional for `integrations.gmail.read → logic.foreach`.

| Input | Type | Description |
|-------|------|-------------|
| `inputArray` | String | JSON array string to iterate over (also accepts `emails`, `items`, etc.) |

| Output | Type | Description |
|--------|------|-------------|
| `items` | String | Full JSON array of all items |
| `count` | Number | Number of items |
| `firstItem` | String | First item JSON string |
| `item_0..N` | String | Per-item outputs (fan-out mode) |
| `_foreach_items` | String | Engine signal in loop mode |
| `results` | JSON | Collected per-item results (loop mode, after engine iteration) |

| Config | Type | Default | Description |
|--------|------|---------|-------------|
| `maxItems` | Number | 50 | Max items to process (cap: 200) |
| `itemVariable` | String | item | Name prefix for outputs |
| `loopMode` | Boolean | false | Enable per-item engine loop. Requires `logic.foreach.end` at end of body. |
| `inheritOutputs` | Boolean | false | When true, each node in the loop body receives all outputs accumulated so far in the iteration (e.g. `ai.extract` outputs available in `integrations.http` without extra wiring). Off by default to keep memory usage flat. |

**Loop body wiring:**
```
gmail.read → logic.foreach → ai.extract → data.db-execute → integrations.http → logic.foreach.end → system.end
```
With `inheritOutputs=true`, `integrations.http` receives `Amount`, `Currency`, etc. from `ai.extract` without a direct edge. With `inheritOutputs=false` (default), wire explicit edges or use direct predecessor outputs only.

---

### `logic.foreach.end`  ? Shipped

**Category:** Logic
**Description:** Marks the end of a ForEach loop body. Wire this after the last node in the loop body when using `logic.foreach` with `loopMode=true`. Passes all inputs through as outputs unchanged. The engine collects outputs from this node as the per-item result.

| Input | Type | Description |
|-------|------|-------------|
| `*` | Any | All inputs are passed through |

| Output | Type | Description |
|--------|------|-------------|
| `*` | Any | All inputs passed through unchanged |

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

### `integrations.gmail.read`  ? Shipped

- **Purpose:** Reads emails from Gmail via OAuth2 refresh token flow. Returns structured email data including subject, sender, date, body, and snippet.
- **Inputs:** none
- **Outputs:**
  - `emails` (`String` � JSON array of `{id, threadId, subject, from, date, body, snippet}`)
  - `count` (`Number`)
- **Config:**
  - `clientId` (`String`, optional if `credentialName` set) — OAuth2 client ID from Google Cloud Console
  - `clientSecret` (`String`, optional if `credentialName` set) — OAuth2 client secret
  - `refreshToken` (`String`, optional if `credentialName` set) — OAuth2 refresh token with Gmail read scope
  - `credentialName` (`String`, optional) — Name of a saved Gmail credential from **Settings → Gmail**. If set, `clientId`/`clientSecret`/`refreshToken` are not needed.
  - `query` (`String`, default: `is:unread`) — Gmail search query
  - `maxResults` (`Number`, default: 10, max: 50) — maximum emails to retrieve
- **Auth:** Uses refresh token → access token exchange; no SDK dependency, pure HTTP.

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

### `data.db-query`  ✅ Shipped

- **Purpose:** Executes a parameterized SELECT query against a relational database and returns result rows as JSON.
- **Inputs:** `connectionString` (`String`, optional — overrides config at runtime)
- **Outputs:** `rows` (`String` — JSON array of row objects), `rowCount` (`Number`)
- **Config:**
  - `provider` (`Enum: postgresql|sqlserver|mysql`, required)
  - `connectionString` (`String`, optional — can be passed as runtime input)
  - `query` (`String`, required — parameterized SQL, use `@paramName` placeholders)
  - `parameters` (`String` — JSON object of bind parameters, e.g. `{"userId": "abc"}`)
  - `timeoutSeconds` (`Number`, default 30)
- **Error codes:** `DB_MISSING_CONNECTION`, `DB_MISSING_QUERY`, `DB_UNKNOWN_PROVIDER` (not retryable), `DB_CONNECTION_FAILED` (retryable), `DB_QUERY_FAILED` (not retryable)

### `data.db-execute`  ✅ Shipped

- **Purpose:** Executes a parameterized INSERT, UPDATE, or DELETE statement and returns the number of rows affected.
- **Inputs:** `connectionString` (`String`, optional — overrides config at runtime)
- **Outputs:** `rowsAffected` (`Number`), `success` (`Boolean`)
- **Config:**
  - `provider` (`Enum: postgresql|sqlserver|mysql`, required)
  - `connectionString` (`String`, optional — can be passed as runtime input)
  - `statement` (`String`, required — parameterized SQL, use `@paramName` placeholders)
  - `parameters` (`String` — JSON object of bind parameters)
  - `timeoutSeconds` (`Number`, default 30)
- **Error codes:** `DB_MISSING_CONNECTION`, `DB_MISSING_STATEMENT`, `DB_UNKNOWN_PROVIDER` (not retryable), `DB_CONNECTION_FAILED` (retryable), `DB_EXECUTE_FAILED` (not retryable)

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

- **Purpose:** Calls any external REST API endpoint with configurable authentication.
- **Inputs:** none
- **Outputs:** `statusCode` (`Number`), `responseBody` (`String`), `success` (`Boolean`)
- **Config:**
  | Key | Type | Description |
  |---|---|---|
  | `url` | String | Target URL (required) |
  | `method` | Enum | `GET` / `POST` / `PUT` / `DELETE` / `PATCH` (default `GET`) |
  | `headers` | String | JSON object of extra request headers |
  | `body` | String | Request body for POST/PUT |
  | `timeoutSeconds` | Number | Request timeout (default `30`) |
  | `authType` | Enum | `none` · `bearer` · `basic` · `api-key` · `oauth2-client-credentials` (default `none`) |

  **Auth-specific config fields:**

  | Auth Type | Config Keys |
  |---|---|
  | `bearer` | `authToken` — the token string (supports `{{placeholder}}`) |
  | `basic` | `authUsername`, `authPassword` (both support `{{placeholder}}`) |
  | `api-key` | `authApiKeyName`, `authApiKeyValue`, `authApiKeyLocation` (`header` or `query`) |
  | `oauth2-client-credentials` | `authTokenUrl`, `authClientId`, `authClientSecret`, `authScope` |

  All credential fields support `{{placeholder}}` syntax — the value is resolved from upstream node inputs at runtime.

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

## Node Inventory (as of 2026-05-26)

| Status | Count | Nodes |
|---|---|---|
| Shipped | 24 | `system.start`, `system.end`, `logic.condition`, `logic.delay`, `logic.switch`, `logic.merge`, `logic.foreach`, `logic.foreach.end`, `human.approval`, `ai.contract-risk-analysis`, `ai.executive-summary`, `ai.classify`, `ai.extract`, `ai.translate`, `documents.extract-pdf`, `integrations.http`, `integrations.slack`, `integrations.webhook-out`, `integrations.email`, `integrations.gmail.read`, `data.set`, `data.json-transform`, `data.db-query`, `data.db-execute` |
| Roadmap | 20+ | See Phase 10-16 items above |

| Tier | Nodes |
| ------ | -------------------------------------------------------------------------------------- |
| High | `ai.summarize`, `document.generate-pdf-report`, `ai.rag-search`, `human.manual-review`, `logic.loop`, `logic.parallel` |
| Medium | `integration.jira.create-ticket`, `document.ocr`, `ai.compare-documents`, `integration.database.query`, `ai.sentiment-analysis` |
| Advanced | `ai.agent-executor`, `ai.multi-agent-review`, `ai.workflow-planner`, `system.error-boundary`, `system.compensation-step` |

---

## Node Configuration Presets

A **preset** is a saved, reusable set of configuration values for a specific node type. Users create presets once and apply them to any node of the matching type in any workflow.

### How It Works

1. Open **Settings ? Presets** in the UI (or call GET /api/presets).
2. Create a preset for a node type (e.g. integrations.http) with a name and a JSON config object.
3. In the workflow designer, open any node of that type � a **Use Preset** dropdown appears at the top of the config panel.
4. Selecting a preset populates all matching config fields instantly.
5. You can still override individual fields after applying the preset.

### API

| Method | Endpoint | Description |
|---|---|---|
| GET | /api/presets?tenantId=...&nodeType=... | List presets, optionally filtered by node type |
| GET | /api/presets/{id}?tenantId=... | Get a single preset |
| POST | /api/presets?tenantId=... | Create a preset ({ name, nodeType, configJson }) |
| PUT | /api/presets/{id}?tenantId=... | Update name or configJson |
| DELETE | /api/presets/{id}?tenantId=... | Delete a preset |

### Example Preset Payload

`json
{
  "name": "Production API � Bearer Auth",
  "nodeType": "integrations.http",
  "configJson": "{\"authType\":\"bearer\",\"timeoutSeconds\":10,\"method\":\"POST\"}"
}
`

### Scope

Presets are **tenant-scoped** � each tenant has its own isolated library. Presets are not shared across tenants.


## Secret References in Node Config

Any string config field in a node can reference a secret from the vault using the {{secret:name}} syntax. The engine resolves these before passing config to the node.

Example: set piKey to {{secret:openai-key}} and the engine will substitute the decrypted value at runtime. Secrets are managed via the Settings page or the /api/secrets endpoints.

---

## Forms

Form nodes are dynamically registered at runtime � one node type per form you create in the **Forms** builder (`/forms`). They appear in the designer palette under the **Forms** category.

### `form.<slug>`  — Dynamic (created via Forms UI)

**Category:** forms
**Description:** Pauses workflow execution and waits for a user to fill out a custom form. Each form field becomes an output key once submitted.

**Example:** A form named "Expense Review" with slug `expense-review` registers as node type `form.expense-review`.

| Input | Type | Description |
|-------|------|-------------|
| *(one per field)* | String | Pre-filled values (optional, passed in on resume) |

| Output | Type | Description |
|--------|------|-------------|
| *(one per field)* | String | User-submitted value, e.g. `amount`, `category`, `notes` |
| `_formSubmitted` | Boolean | `true` once the form has been submitted |

**Config:** None — behaviour is fully defined by the form schema.

**Engine behaviour:**
1. First execution → returns `WaitingForApproval`; workflow pauses and an `ApprovalRequest` record is created with `_formId`, `_formName`, and `_formFields` in `payloadJson`
2. The approval appears in the **Approval Inbox** (`/approvals`). The inbox detail page detects `_formId` and renders the form fields for the user to fill
3. User fills the form and clicks **Submit Form** → `POST /api/forms/{id}/submit` resumes the execution
4. Engine resumes → `Succeeded` with each field value as a named output

> **Tip:** The form can also be filled via the public fill page: `/forms/<id>/fill?executionId=...&nodeExecutionId=...` (no login required). A link appears in the execution timeline when the node is waiting.

> **Worker startup note:** Form nodes are registered at worker startup. If you create a new form while the worker is running, restart the worker to make the new `form.<slug>` type available. Hot-reload is a planned improvement.

**Using outputs downstream:**
```
{{amount}}    → numeric value submitted by user
{{category}}  → selected option
{{notes}}     → free-text notes
```


---

## Integrations � External Trigger Nodes

### `integrations.wait-for-webhook`  ? Shipped

**Category:** integrations
**Description:** Pauses the workflow and waits for an external system to POST data to a unique URL. Once called, the workflow resumes with the external payload as outputs.

| Config | Type | Default | Description |
|--------|------|---------|-------------|
| `timeoutSeconds` | Number | � | Optional expiry in seconds. After expiry the token is invalidated. |

| Output | Type | Description |
|--------|------|-------------|
| `_correlationToken` | String | Unique token for the resume URL |
| `_resumeUrl` | String | Full path: `/api/webhooks/resume/{token}` |
| `_resumedAt` | String | ISO timestamp when resumed |
| *(any field POSTed by external)* | String | All fields from the external POST body |

**Resume URL** is shown in the execution timeline when the node is paused.

**Example:**
```
Start ? db-query ? integrations.wait-for-webhook ? integrations.http ? End
```
Workflow pauses at `wait-for-webhook`. ERP system POSTs to the resume URL with `{ "confirmed": true, "poNumber": "PO-999" }`. Workflow resumes; `{{confirmed}}` and `{{poNumber}}` flow to the HTTP node.

---

### `integrations.external-gate`  ? Shipped

**Category:** integrations
**Description:** Pauses the workflow and waits for an external system to approve or reject. Acts like `human.approval` but driven by an API call instead of a UI click.

| Config | Type | Default | Description |
|--------|------|---------|-------------|
| `timeoutSeconds` | Number | � | Optional expiry |

| Output | Type | Description |
|--------|------|-------------|
| `approved` | Boolean | `true` if approved, `false` if rejected |
| `reason` | String | Optional reason from external system |
| `_correlationToken` | String | Token used to call the gate endpoint |
| `_resumedAt` | String | ISO timestamp |
| `data.*` | String | Any fields inside `"data": {}` in the gate POST body |

**Gate URL:** `/api/webhooks/gate/{token}`

