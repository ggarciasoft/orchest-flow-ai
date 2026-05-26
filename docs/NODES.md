# Node Catalog

This is the canonical list of node types in OrchestFlowAI. **MVP** nodes ship in the first release; the rest are roadmap.

Each entry has:
- `type` â€” unique id
- **Purpose**
- **Inputs**, **Outputs**, **Config**
- **Status** â€” MVP / Phase N

---

## System

### `system.start`  Â· MVP

- **Purpose:** Entry point of every workflow. Surfaces workflow inputs.
- **Inputs:** none
- **Outputs:** all workflow inputs as named outputs
- **Config:** none

### `system.end`  Â· MVP

- **Purpose:** Terminal node. Marks workflow completion.
- **Inputs:** any final values to persist as workflow output
- **Outputs:** none
- **Config:** none

### `system.error-boundary`  Â· Phase 11

- **Purpose:** Catch errors from a sub-graph and route to handler nodes.

### `system.compensation-step`  Â· Phase 11

- **Purpose:** Define rollback behavior for a node.

---

## Documents

### `document.extract-pdf-text`  Â· MVP

- **Purpose:** Extract plain text from a PDF document.
- **Inputs:** `document` (`DocumentRef`, required)
- **Outputs:** `text` (`String`), `pageCount` (`Number`)
- **Config:** `ocrFallback` (`Boolean`, default `false`)

### `document.ocr`  Â· Phase 11

- **Purpose:** OCR scanned documents/images.

### `document.classify`  Â· Phase 11

- **Purpose:** Classify document by type (invoice, contract, NDA, â€¦).

### `document.split`  Â· Phase 11

- **Purpose:** Split multi-document PDFs by detected boundaries.

### `document.extract-tables`  Â· Phase 11

- **Purpose:** Extract structured tables from documents.

### `document.generate-pdf-report`  Â· Phase 11

- **Purpose:** Render a templated PDF report from data.

---

## AI

### `ai.contract-risk-analysis`  Â· MVP

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

### `ai.executive-summary`  Â· MVP

- **Purpose:** Generate a 1â€“2 paragraph executive summary.
- **Inputs:** `text` (`String`, required)
- **Outputs:** `summary` (`String`)
- **Config:** `model`, `maxWords` (default 250), `tone` (formal|neutral|friendly)

### `ai.summarize`  Â· Phase 11

- **Purpose:** Generic text summarization with configurable length/style.

### `ai.classify`  âœ… Shipped

- **Purpose:** Classify text into configured categories using an LLM.
- **Inputs:** `text` (`String`, required)
- **Outputs:** `category` (`String`), `confidence` (`String`), `rawResponse` (`String`)
- **Config:** `categories` (comma-separated, required), `model`, `instructions`

### `ai.extract`  âœ… Shipped

- **Purpose:** Extract structured fields from unstructured text using an LLM.
- **Inputs:** `text` (`String`, required)
- **Outputs:** `extractedJson` (`Json`), plus individual field outputs
- **Config:** `fields` (comma-separated, required), `model`

### `ai.extract-entities`  Â· Phase 11

- **Purpose:** Extract named entities (people, orgs, dates, money, â€¦).

### `ai.sentiment-analysis`  Â· Phase 11

- **Purpose:** Score sentiment / emotion of input text.

### `ai.compare-documents`  Â· Phase 11

- **Purpose:** Diff two documents semantically and highlight differences.

### `ai.generate-email`  Â· Phase 11

- **Purpose:** Compose an email from structured inputs and tone settings.

### `ai.agent-executor`  Â· Phase 13

- **Purpose:** Execute an agent loop with tools, memory, and stop conditions.

### `ai.rag-search`  Â· Phase 12

- **Purpose:** Search a knowledge base and return cited passages.

### `ai.translate`  âœ… Shipped

- **Purpose:** Translate text into a configured target language using an LLM.
- **Inputs:** `text` (`String`, required)
- **Outputs:** `translatedText` (`String`), `targetLanguage` (`String`)
- **Config:** `targetLanguage` (required), `model`

### `ai.policy-check`  Â· Phase 11

- **Purpose:** Check text against a policy spec; return compliant/violations.

---

## Logic

### `logic.condition`  Â· MVP

- **Purpose:** Evaluate a boolean expression and route to the matching branch.
- **Inputs:** all upstream outputs are available in the expression scope
- **Outputs:** `result` (`Boolean`)
- **Config:** `expression` (`String`, required)

### `logic.switch`  âœ… Shipped

- **Purpose:** Multi-way routing by a key value matched against configured cases.
- **Inputs:** `value` (`String`, required)
- **Outputs:** `matchedCase` (`String`), `matched` (`Boolean`)
- **Config:** `cases` (comma-separated, required)

### `logic.foreach`  ? Shipped

- **Purpose:** Fan-out node — expands a JSON array into indexed outputs (`item_0`..`item_N`). Use to pass individual items downstream without engine-level loop support.
- **Inputs:** `inputArray` (`String`, required — JSON array string)
- **Outputs:** `items` (`String` — full JSON array), `count` (`Number`), `firstItem` (`String` — first item JSON), `item_0`..`item_N` (per-item JSON strings)
- **Config:**
  - `itemVariable` (`String`, default: `item`) — prefix for per-item output keys
  - `maxItems` (`Number`, default: 50, max: 200) — safety cap on expansion
- **Note:** This is a fan-out (not a loop). All items are emitted simultaneously. For sequential per-item processing, wire downstream nodes to individual `item_N` outputs.

### `logic.loop`  Â· Phase 10

- **Purpose:** Iterate over an array with bounded iterations.

### `logic.parallel`  Â· Phase 10

- **Purpose:** Fan out to N branches; join when all complete.

### `logic.delay`  âœ… Shipped

- **Purpose:** Pause the workflow for a configured duration.
- **Inputs:** none
- **Outputs:** `delayedMs` (`Number`)
- **Config:** `durationMs` (`Number`, required, default 1000)

### `logic.retry-policy`  Â· Phase 10

- **Purpose:** Override default retry behavior for a downstream node.

### `logic.merge`  âœ… Shipped

- **Purpose:** Synchronization point that collects outputs from multiple upstream branches and forwards all as outputs.
- **Inputs:** dynamic
- **Outputs:** all inputs forwarded
- **Config:** none

---

### `integrations.gmail.read`  ? Shipped

- **Purpose:** Reads emails from Gmail via OAuth2 refresh token flow. Returns structured email data including subject, sender, date, body, and snippet.
- **Inputs:** none
- **Outputs:**
  - `emails` (`String` — JSON array of `{id, threadId, subject, from, date, body, snippet}`)
  - `count` (`Number`)
- **Config:**
  - `clientId` (`String`, required) — OAuth2 client ID from Google Cloud Console
  - `clientSecret` (`String`, required) — OAuth2 client secret
  - `refreshToken` (`String`, required) — OAuth2 refresh token with Gmail read scope
  - `query` (`String`, default: `is:unread`) — Gmail search query
  - `maxResults` (`Number`, default: 10, max: 50) — maximum emails to retrieve
- **Auth:** Uses refresh token ? access token exchange; no SDK dependency, pure HTTP.

## Data  âœ… Shipped

### `data.set`  âœ… Shipped

- **Purpose:** Sets workflow variables with optional `{{placeholder}}` substitution from node inputs.
- **Inputs:** none
- **Outputs:** dynamic â€” all keys from the `variables` config
- **Config:** `variables` (JSON keyâ†’value map, required)

### `data.json-transform`  âœ… Shipped

- **Purpose:** Maps/reshapes a JSON object using dot-notation path mappings.
- **Inputs:** `json` (`String`, required)
- **Outputs:** `transformedJson` (`Json`) + individual mapped fields
- **Config:** `mapping` (JSON outputâ†’input path map, required)

### `data.db-query`  âœ… Shipped

- **Purpose:** Executes a parameterized SELECT query against a relational database and returns result rows as JSON.
- **Inputs:** `connectionString` (`String`, optional â€” overrides config at runtime)
- **Outputs:** `rows` (`String` â€” JSON array of row objects), `rowCount` (`Number`)
- **Config:**
  - `provider` (`Enum: postgresql|sqlserver|mysql`, required)
  - `connectionString` (`String`, optional â€” can be passed as runtime input)
  - `query` (`String`, required â€” parameterized SQL, use `@paramName` placeholders)
  - `parameters` (`String` â€” JSON object of bind parameters, e.g. `{"userId": "abc"}`)
  - `timeoutSeconds` (`Number`, default 30)
- **Error codes:** `DB_MISSING_CONNECTION`, `DB_MISSING_QUERY`, `DB_UNKNOWN_PROVIDER` (not retryable), `DB_CONNECTION_FAILED` (retryable), `DB_QUERY_FAILED` (not retryable)

### `data.db-execute`  âœ… Shipped

- **Purpose:** Executes a parameterized INSERT, UPDATE, or DELETE statement and returns the number of rows affected.
- **Inputs:** `connectionString` (`String`, optional â€” overrides config at runtime)
- **Outputs:** `rowsAffected` (`Number`), `success` (`Boolean`)
- **Config:**
  - `provider` (`Enum: postgresql|sqlserver|mysql`, required)
  - `connectionString` (`String`, optional â€” can be passed as runtime input)
  - `statement` (`String`, required â€” parameterized SQL, use `@paramName` placeholders)
  - `parameters` (`String` â€” JSON object of bind parameters)
  - `timeoutSeconds` (`Number`, default 30)
- **Error codes:** `DB_MISSING_CONNECTION`, `DB_MISSING_STATEMENT`, `DB_UNKNOWN_PROVIDER` (not retryable), `DB_CONNECTION_FAILED` (retryable), `DB_EXECUTE_FAILED` (not retryable)

---

## Human

### `human.approval`  Â· MVP

- **Purpose:** Pause workflow and request a human approve/reject decision.
- **Inputs:** `payload` (`Json`) â€” context shown to the approver
- **Outputs:** `decision` (`Enum: approved|rejected`), `comment` (`String`), `decidedBy` (`String`), `decidedAt` (`String`)
- **Config:**
  - `requiredWhen` (expression; if false, node is auto-skipped)
  - `assignees` (`Json`: users or roles)
  - `slaMinutes` (`Number`, optional)
  - `title` (`String`)

### `human.manual-review`  Â· Phase 11

- **Purpose:** Like approval, but the user edits / annotates a payload before continuing.

### `human.assign-task`  Â· Phase 11

- **Purpose:** Create a generic human task; outputs include completion data.

---

## Integrations  âœ… Partial â€” 4 nodes shipped

### `integrations.http`  âœ… Shipped

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
  | `authType` | Enum | `none` Â· `bearer` Â· `basic` Â· `api-key` Â· `oauth2-client-credentials` (default `none`) |

  **Auth-specific config fields:**

  | Auth Type | Config Keys |
  |---|---|
  | `bearer` | `authToken` â€” the token string (supports `{{placeholder}}`) |
  | `basic` | `authUsername`, `authPassword` (both support `{{placeholder}}`) |
  | `api-key` | `authApiKeyName`, `authApiKeyValue`, `authApiKeyLocation` (`header` or `query`) |
  | `oauth2-client-credentials` | `authTokenUrl`, `authClientId`, `authClientSecret`, `authScope` |

  All credential fields support `{{placeholder}}` syntax â€” the value is resolved from upstream node inputs at runtime.

### `integrations.slack`  âœ… Shipped

- **Purpose:** Sends a message to Slack via an incoming webhook URL.
- **Outputs:** `sent` (`Boolean`)
- **Config:** `webhookUrl` (required), `message` (supports `{{placeholders}}`), `channel`

### `integrations.webhook-out`  âœ… Shipped

- **Purpose:** POSTs execution context and optional node inputs to a configured URL.
- **Outputs:** `statusCode` (`Number`), `sent` (`Boolean`)
- **Config:** `url` (required), `includeInputs` (all|none)

### `integrations.email`  âœ… Shipped

- **Purpose:** Sends an email via SMTP. Subject and body support `{{placeholder}}` substitution.
- **Outputs:** `sent` (`Boolean`), `to` (`String`)
- **Config:** `to`, `subject`, `body` (all required + `{{placeholders}}`), `smtpHost`, `smtpPort`

### `integration.teams.send-message`  Â· Phase 12+

### `integration.jira.create-ticket`

### `integration.database.query`

### `integration.salesforce.create-record`  Â· Phase 16

### `integration.sharepoint.upload-file`  Â· Phase 16

---

## Node Inventory (as of 2026-05-24)

| Status | Count | Nodes |
|---|---|---|
| Shipped | 23 | `system.start`, `system.end`, `logic.condition`, `logic.delay`, `logic.switch`, `logic.merge`, `logic.foreach`, `human.approval`, `ai.contract-risk-analysis`, `ai.executive-summary`, `ai.classify`, `ai.extract`, `ai.translate`, `documents.extract-pdf`, `integrations.http`, `integrations.slack`, `integrations.webhook-out`, `integrations.email`, `integrations.gmail.read`, `data.set`, `data.json-transform`, `data.db-query`, `data.db-execute` |
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
3. In the workflow designer, open any node of that type ï¿½ a **Use Preset** dropdown appears at the top of the config panel.
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
  "name": "Production API ï¿½ Bearer Auth",
  "nodeType": "integrations.http",
  "configJson": "{\"authType\":\"bearer\",\"timeoutSeconds\":10,\"method\":\"POST\"}"
}
`

### Scope

Presets are **tenant-scoped** ï¿½ each tenant has its own isolated library. Presets are not shared across tenants.

