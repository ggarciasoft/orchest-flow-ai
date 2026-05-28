# API Reference

Base URL (local): `http://localhost:5080`
Versioning: prefix all endpoints with `/api`. Breaking changes go to `/api/v2`.

All endpoints (except auth) require:

- `Authorization: Bearer <jwt>`
- Tenant is resolved from the token; do not pass `tenant_id` explicitly.

Responses are JSON. Errors follow [RFC 7807 Problem Details](https://www.rfc-editor.org/rfc/rfc7807):

```json
{
  "type": "https://OrchestFlowAI.dev/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "detail": "definition.nodes[2].config.expression is required",
  "errors": [{ "path": "definition.nodes[2].config.expression", "message": "required" }]
}
```

---

## 1. Workflows

### `GET /api/workflows`
List workflows for the tenant.

Query: `?search=`, `?page=`, `?pageSize=`

Response:
```json
{
  "items": [
    { "id": "...", "name": "Contract Review", "activeVersion": 3, "updatedAt": "..." }
  ],
  "page": 1, "pageSize": 20, "total": 12
}
```

### `POST /api/workflows`
Create a workflow (creates version 1).

Body:
```json
{
  "name": "string",
  "description": "string",
  "definition": { /* see ARCHITECTURE.md §6 */ }
}
```

201 with `{ id, activeVersion }`.

### `GET /api/workflows/{workflowId}`
Get the workflow with its active version definition.

### `PUT /api/workflows/{workflowId}`
Update workflow metadata — does **not** modify versions.

Body:
```json
{ "name": "My Workflow", "description": "Optional description" }
```

Response `200`: updated workflow object.

### `POST /api/workflows/{workflowId}/clone`
Duplicate a workflow. Creates a new workflow named `"Copy of {name}"` with the source's active version definition as v1 (immediately activated). Copies trigger type, retry policy, and cron/webhook config.

Response `201`: new workflow object. The clone opens ready in the designer.

### `DELETE /api/workflows/{workflowId}`
Soft-delete a workflow.

### `POST /api/workflows/{workflowId}/versions`
Create a new version (drafts a new definition).

Body: `{ "definition": { ... } }`

Response: `{ "id": "...", "versionNumber": 2 }`

### `POST /api/workflows/{workflowId}/versions/{versionId}/activate`
Mark a version as active. Only one active version per workflow.

Response: `204 No Content`

### `POST /api/workflows/{workflowId}/validate`
Validate the workflow's current definition without saving. Returns the engine's validation report.

---

## 2. Executions

### `POST /api/workflows/{workflowId}/execute`
Trigger an execution.

Body:
```json
{ "input": { "documentId": "uuid", "any": "json" } }
```

Response: `{ "executionId": "uuid", "status": "Queued" }`

### `GET /api/executions/{executionId}`
Get execution summary.

Response:
```json
{
  "id": "uuid",
  "workflowId": "uuid",
  "workflowVersionId": "uuid",
  "status": "Running",
  "startedAt": "...",
  "completedAt": null,
  "triggeredBy": "uuid",
  "input": { "..." : "..." },
  "output": null,
  "errorMessage": null
}
```

### `GET /api/executions/{executionId}/timeline`
Returns the per-node timeline.

Response:
```json
{
  "executionId": "uuid",
  "nodes": [
    {
      "nodeExecutionId": "uuid",
      "nodeId": "extractPdf",
      "nodeType": "document.extract-pdf-text",
      "status": "Succeeded",
      "startedAt": "...",
      "completedAt": "...",
      "input": { "..." : "..." },
      "output": { "..." : "..." },
      "errorMessage": null,
      "retryCount": 0
    }
  ]
}
```

### `POST /api/executions/{executionId}/cancel`
Cancel a running or paused execution.

### `GET /api/executions`
List executions with filters.

Query: `?workflowId=`, `?status=`, `?from=`, `?to=`, `?page=`, `?pageSize=`

---

## 3. Approvals

### `GET /api/approvals`
List pending approvals visible to the caller.

Query: `?status=`, `?workflowId=`, `?page=`, `?pageSize=`

### `GET /api/approvals/{approvalId}`
Get an approval request payload.

### `POST /api/approvals/{approvalId}/approve`
Body: `{ "comment": "string" }`

### `POST /api/approvals/{approvalId}/reject`
Body: `{ "comment": "string" }`

Both endpoints persist the decision, mark the node execution `Succeeded` with `{ decision, comment }`, and enqueue resume.

---

## 4. Documents

### `POST /api/documents/upload`
Multipart upload.

Form fields:
- `file` — required
- `meta` — optional JSON string

Response:
```json
{
  "id": "uuid",
  "filename": "contract.pdf",
  "mimeType": "application/pdf",
  "sizeBytes": 123456,
  "sha256": "..."
}
```

### `GET /api/documents/{documentId}`
Get document metadata.

### `GET /api/documents/{documentId}/content`
Download the file (streamed). Authorization checks tenancy + ownership.

---

## 5. Node Catalog

### `GET /api/nodes/catalog`
Returns all registered node descriptors:

```json
{
  "nodes": [
    {
      "type": "document.extract-pdf-text",
      "displayName": "Extract PDF Text",
      "description": "...",
      "category": "documents",
      "version": "0.1.0",
      "iconKey": "file-pdf",
      "inputs":  [ { "key": "document", "type": "DocumentRef", "required": true } ],
      "outputs": [ { "key": "text", "type": "String" } ],
      "configuration": [ { "key": "ocrFallback", "type": "Boolean", "defaultValue": false } ]
    }
  ]
}
```

The frontend uses this to render the node palette and configuration drawer.

### `GET /api/nodes/models`
Returns available LLM models for the configured providers. Used to populate model dropdowns on AI nodes.

Response:
```json
{
  "models": [
    { "id": "gpt-4o", "displayName": "GPT-4o", "provider": "openai" },
    { "id": "gpt-4o-mini", "displayName": "GPT-4o Mini", "provider": "openai" },
    { "id": "claude-3-5-sonnet-20241022", "displayName": "Claude 3.5 Sonnet", "provider": "anthropic" }
  ]
}
```

Node descriptors with `OptionsSource: "llm-models"` on the `model` config field will fetch from this endpoint to populate their dropdown.

---

## 6. Health & Meta

### `GET /api/health`
Liveness + dependency probes.

### `GET /api/version`
Service build version.

---

## 7. Auth (MVP)

MVP ships with email/password JWT auth. Endpoints:

- `POST /api/auth/login` → `{ token, user }`
- `POST /api/auth/logout`
- `GET /api/auth/me`

Token expiry: when the client receives a `401` or detects the token is expired (checked via `isTokenExpired()` in auth.ts), `apiFetch` automatically redirects to `/login`.

SSO/OIDC arrives in Phase 14.

---

## 8. Error Codes

| Code                | HTTP | Meaning                                         |
| ------------------- | ---- | ----------------------------------------------- |
| `validation`        | 400  | Request validation failed                       |
| `not_found`         | 404  | Resource missing or not visible to tenant       |
| `forbidden`         | 403  | Authenticated but not authorized                |
| `unauthorized`      | 401  | Missing/invalid auth                            |
| `conflict`          | 409  | State conflict (e.g. version already activated) |
| `unprocessable`     | 422  | Workflow definition failed engine validation    |
| `rate_limited`      | 429  | Exceeded per-tenant or provider rate limit      |
| `internal`          | 500  | Unhandled server error                          |

---

## 9. Gmail Credentials

Manage saved Gmail OAuth2 credentials for use with the `integrations.gmail.read` node.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/gmail/auth/start` | JWT | Start OAuth2 flow; redirects to Google consent screen |
| GET | `/api/gmail/callback` | Public | OAuth2 callback; exchanges code for tokens and stores credential |
| GET | `/api/gmail/credentials` | JWT | List saved credentials (name, email — no secrets) |
| DELETE | `/api/gmail/credentials/{id}` | JWT | Delete a credential |

### Starting the OAuth Flow

```
GET /api/gmail/auth/start?name=my-gmail&clientId=...&clientSecret=...
```

Redirects the browser to Google's consent screen. After the user grants access, Google calls `/api/gmail/callback` which stores the credential under the provided `name`.

### Using a Saved Credential

In a `integrations.gmail.read` node, set `credentialName: "my-gmail"` instead of providing `clientId`, `clientSecret`, and `refreshToken` inline.

---

## 10. Settings

Platform settings for the tenant (AI provider config, default models, etc.).

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/settings` | JWT | Get platform settings (sensitive values masked) |
| PUT | `/api/settings` | JWT | Update settings; empty/omitted values are ignored |
| POST | `/api/settings/test/openai` | JWT | Test the configured OpenAI connection |

### GET /api/settings — Response

```json
{
  "llm.openai.apiKey": "sk-...(masked)",
  "llm.defaultModel": "gpt-4o-mini",
  "llm.defaultProvider": "openai"
}
```

### PUT /api/settings — Body

```json
{
  "llm.openai.apiKey": "sk-abc123",
  "llm.defaultModel": "gpt-4o"
}
```

Settings changes take effect immediately (hot-reloaded via `OpenAIApiKeyHolder`) — no restart required.

### POST /api/settings/test/openai — Response

```json
{ "success": true, "model": "gpt-4o-mini", "latencyMs": 312 }
```

---

## 11. Secrets

The secret vault stores sensitive values (API keys, tokens, passwords) encrypted at rest. Reference them in node config using `{{secret:name}}` syntax.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/secrets` | JWT | List secret names and metadata (values never returned) |
| POST | `/api/secrets` | JWT | Create a secret (value encrypted before storage) |
| PUT | `/api/secrets/{id}` | JWT | Update a secret's name or value |
| DELETE | `/api/secrets/{id}` | JWT | Delete a secret |

### Secret Reference Syntax

In any string config field of a node:

```
{{secret:my-api-key}}
{{secret:openai-token}}
{{secret:db-connection}}
```

The engine resolves all `{{secret:name}}` tokens before passing config to the node. Secret values are never stored in workflow definitions, logs, or API responses.

### POST /api/secrets — Body

```json
{ "name": "openai-key", "value": "sk-abc123" }
```

### GET /api/secrets — Response

```json
{
  "items": [
    { "id": "uuid", "name": "openai-key", "createdAt": "...", "updatedAt": "..." }
  ]
}
```

See also: [Secret Resolution in WORKFLOW-ENGINE.md](./WORKFLOW-ENGINE.md) and [Secret Vault in SECURITY.md](./SECURITY.md).

---

## 12. Node Presets

Presets are saved, reusable config snapshots for a node type. See [NODES.md](./NODES.md) for full documentation.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/presets?nodeType=...` | List presets, optionally filtered by node type |
| GET | `/api/presets/{id}` | Get a single preset |
| POST | `/api/presets` | Create a preset |
| PUT | `/api/presets/{id}` | Update a preset |
| DELETE | `/api/presets/{id}` | Delete a preset |

---

## 13. Idempotency

Mutating endpoints (`POST /api/workflows/{id}/execute`, approvals) accept an optional header:

```
Idempotency-Key: <uuid>
```

If the same key is replayed within 24h, the original response is returned without re-executing.

---

## 14. Pagination

Standard envelope:

```json
{ "items": [ "..." ], "page": 1, "pageSize": 20, "total": 137 }
```

Default page size 20, max 100.

---

## 15. AI Workflow Assistant

### Generate / Update Workflow

`
POST /api/workflows/ai-assist
Authorization: Bearer <token>
Content-Type: application/json
`

**Request body:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| prompt | string | ? | Plain-language description of what to build or change |
| currentDefinitionJson | string | ? | Current canvas definition JSON. When provided, the AI updates the existing workflow instead of creating a new one |
| workflowName | string | ? | Name for the workflow (used in the generated definition) |

**Example � create new:**
```json
{
  "prompt": "Read Gmail emails labelled recibos-pagos-facturas, extract Amount, Currency, Date and Store using the financial preset, save each row to PostgreSQL",
  "workflowName": "Gmail Receipt Extractor"
}
```

**Example � update existing:**
```json
{
  "prompt": "Add an HTTP webhook notification after the database insert step",
  "currentDefinitionJson": "{ ...current canvas definition... }"
}
```

**Response 200:**
```json
{
  "definition": { ...WorkflowDefinition... },
  "explanation": "Built a Gmail ? ForEach ? ai.extract ? db-execute chain. ForEach is in loop mode with the financial preset configured on ai.extract.",
  "changes": [
    "Added integrations.http node after data.db-execute",
    "Wired db-execute ? http ? foreach.end"
  ]
}
```

**Errors:**
| Status | Meaning |
|--------|---------|
| 400 | Prompt is empty |
| 500 | LLM generation failed (check error field in body) |

**Notes:**
- Requires EditorOrAbove role
- The returned definition is not saved � the client previews it and calls POST /api/workflows/{id}/versions + activate when accepted
- The AI is given a compact node catalog (all types, key inputs/outputs/config) so it knows what nodes exist
- Current definition is injected as context when updating; unchanged nodes are preserved

---

## 16. Custom Forms

Forms are user-defined schemas that become workflow nodes (`form.<slug>`). When execution reaches a form node the workflow pauses until a user fills and submits the form, then resumes with each field value available as an output.

### List forms

```
GET /api/forms
Authorization: Bearer <token>
```

Response `200`: `[{ id, name, slug, description, fields, createdAt, updatedAt }]`

### Get form

```
GET /api/forms/{id}
```

### Create form

```
POST /api/forms
```

Body:
```json
{
  "name": "Expense Review",
  "slug": "expense-review",
  "description": "Human review of extracted expense data",
  "fields": [
    { "key": "amount",   "label": "Amount",   "type": "number", "required": true },
    { "key": "category", "label": "Category", "type": "select", "required": true, "options": ["Food","Transport","Other"] },
    { "key": "notes",    "label": "Notes",    "type": "text",   "required": false }
  ]
}
```

Field types: `text` | `number` | `select` | `date` | `email` | `boolean`

> **`slug` is required** and must be unique per tenant. It determines the node type: `form.<slug>`. Use lowercase letters, numbers, and hyphens only.

After creation, the node `form.expense-review` immediately appears in the workflow designer node catalog under the **Forms** category.

### Update form

```
PUT /api/forms/{id}
```

Same body as create. **Each save automatically creates a new version** (version number increments) and activates it. The form's live `FieldsJson` always reflects the active version.

### Form versions

Forms are versioned. Every create or update snapshots the field definitions as an immutable `FormVersion` record. Users can browse, compare, and activate past versions to roll back.

#### List versions

```
GET /api/forms/{id}/versions
Authorization: Bearer <token>
```

Response `200`:
```json
[
  { "id": "...", "versionNumber": 3, "isActive": true,  "createdBy": "...", "createdAt": "...", "fieldsJson": "[...]" },
  { "id": "...", "versionNumber": 2, "isActive": false, "createdBy": "...", "createdAt": "...", "fieldsJson": "[...]" },
  { "id": "...", "versionNumber": 1, "isActive": false, "createdBy": "...", "createdAt": "...", "fieldsJson": "[...]" }
]
```

Ordered newest-first. `isActive` marks the version currently in use by the engine.

#### Activate version

```
POST /api/forms/{id}/versions/{versionId}/activate
Authorization: Bearer <token>
```

Response `204`. Deactivates all other versions, sets the target version as active, and syncs `Form.FieldsJson` to that version's field definitions. The worker and designer catalog update within the next polling interval (≤ 30 s).

> **Rule:** Workflows always use the form's **active version** at execution time. Activating an older version is a full rollback — the engine will use those fields on the next execution.

### Delete form

```
DELETE /api/forms/{id}
```

Soft delete � form is removed from the catalog.

### Get fill schema (public)

```
GET /api/forms/{id}/fill?executionId={execId}&nodeExecutionId={nodeId}
```

`AllowAnonymous` � returns the form schema so the fill page can render it. The `executionId` and `nodeExecutionId` are embedded in the fill link shown in the execution timeline.

### Submit form

```
POST /api/forms/{id}/submit
```

Body:
```json
{
  "workflowExecutionId": "...",
  "nodeExecutionId": "...",
  "values": {
    "amount": 150.00,
    "category": "Food",
    "notes": "Team lunch"
  }
}
```

On success (`204`): creates a `FormSubmission` record and resumes the paused workflow execution. Each submitted value is injected as a node output � `{{amount}}`, `{{category}}`, `{{notes}}` become available to downstream nodes.

### Form node behaviour in engine

| State | Engine action |
|-------|---------------|
| First execution | Returns `WaitingForApproval` → workflow pauses; an `ApprovalRequest` record is created with `_formId`, `_formName`, and `_formFields` in `payloadJson` |
| Approval inbox | The approval detail page detects `_formId` in the payload and renders the form fields instead of the standard Approve/Reject buttons |
| User submits form | `POST /api/forms/{id}/submit` creates a `FormSubmission` and resumes the execution via the resume queue |
| Resume (after submit) | Returns `Succeeded` with each field value as a named output key |

> **Hot-reload:** The worker polls the database every **30 seconds** (`WorkerFormNodeRegistrar.RefreshIntervalSeconds`). New, updated, or deleted forms are reflected automatically — no worker restart required.


---
| First execution | Returns `WaitingForApproval` ? workflow pauses |
| Resume (after submit) | Returns `Succeeded` with each field as an output key |


---

## 17. External Webhooks (Correlation Tokens)

These endpoints are **public (no auth required)** � designed to be called by external systems.

### Resume a paused workflow

```
POST /api/webhooks/resume/{token}
Content-Type: application/json
```

Body: any JSON object. All fields become node outputs.

```json
{ "orderId": "ORD-123", "status": "confirmed", "total": 150.00 }
```

Response:
- `200 { "status": "resumed" }` � workflow resumed, body fields available as `{{orderId}}` etc.
- `404` � token not found
- `410` � token already used or expired
- `400` � token is a gate token, not a wait token

The `token` value is available in the execution timeline when a `integrations.wait-for-webhook` node is paused (`_correlationToken` output).

### Approve/reject an external gate

```
POST /api/webhooks/gate/{token}
Content-Type: application/json
```

Body:
```json
{
  "approved": true,
  "reason": "Looks good",
  "data": { "approvedBy": "john@example.com", "notes": "Verified amount" }
}
```

Response:
- `200 { "status": "resumed" }` � workflow resumed with outputs `approved`, `reason`, `data.*` fields
- `404` / `410` / `400` � same as above

