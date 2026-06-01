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
Cancel a Queued, Running, or Paused execution.

- Returns **204 NoContent** on success.
- Returns **409 Conflict** if the execution is already in a terminal state (Completed, Failed, Cancelled).
- Also cancels any pending `ApprovalRequest` for that execution so it is removed from the approval inbox.

Requires `EditorOrAbove` role.

### `GET /api/executions`
List executions with filters and pagination.

Query: `?status=`, `?search=` (matches CorrelationId), `?page=`, `?pageSize=`

Response: `PagedResponse<WorkflowExecution>` — see §14 Pagination.

---

## 3. Approvals

### `GET /api/approvals`
List **Pending** approvals for the caller's tenant.

Query: `?page=`, `?pageSize=`

Response: `PagedResponse<ApprovalRequest>`

### `GET /api/approvals/{approvalId}`
Get an approval request by id (enriched with workflow name, version, and form version).

### `GET /api/approvals/by-execution/{executionId}`
Get the **Pending** approval for a specific workflow execution.
Returns `404` when no pending approval exists (e.g. already decided or execution was cancelled).

### `POST /api/approvals/{approvalId}/approve`
Body: `{ "comment": "string" }`

### `POST /api/approvals/{approvalId}/reject`
Body: `{ "comment": "string" }`

Both endpoints persist the decision and enqueue resume so the workflow continues.

### `POST /api/approvals/{approvalId}/select-document`
Body: `{ "documentId": "guid", "filename": "string", "mimeType": "string", "sizeBytes": number, "sha256": "string" }`

Response: `ApprovalRequestResponse`

Selects a document for a document selection approval request, approves it, and resumes the workflow with the document metadata as outputs.

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

MVP ships with email/password JWT auth.

### `POST /api/auth/register` _(public)_
Create a new account. Returns `201` with a JWT token so the user is logged in immediately. A welcome email is sent asynchronously (best-effort).

Body:
```json
{ "displayName": "Jane Smith", "email": "jane@example.com", "password": "mypassword" }
```

- Password minimum: **8 characters**
- Returns `400` for missing/invalid fields or short password
- Returns `409 Conflict` if the email is already registered
- Creates a new isolated tenant for the registering user (who becomes `Admin`)

### `POST /api/auth/login` _(public)_
Authenticate with email + password. Returns a JWT token.

Body: `{ "email": "string", "password": "string" }`

- Returns `401` for unknown email or wrong password (same message to prevent enumeration)

### `GET /api/auth/me` _(Authorized)_
Returns the currently authenticated user's profile.

Response: `{ id, email, displayName, role }`

Token expiry: when the client receives a `401` or detects the token is expired (checked via `isTokenExpired()` in auth.ts), `apiFetch` automatically redirects to `/login`.

SSO/OIDC arrives in Phase 14.

---

## 7a. Tenant Management

### `GET /api/tenants/{id}` _(ViewerOrAbove)_
Get tenant info. Must be the caller's own tenant.

### `POST /api/tenants` _(AdminOnly)_
Create a tenant workspace. Body: `{ "name": "string" }`

---

### Team Members

#### `GET /api/tenants/{id}/members` _(AdminOnly)_
List all users in the tenant.

Response: array of
```json
{ "id": "uuid", "email": "user@example.com", "displayName": "Jane", "role": "Editor", "createdAt": "..." }
```

#### `PUT /api/tenants/{id}/members/{userId}/role` _(AdminOnly)_
Change a member's role.

Body: `{ "role": "Viewer|Editor|Admin|Approver" }`

- Returns `400` if the caller tries to change their own role
- Returns `400` for unrecognized role strings

#### `DELETE /api/tenants/{id}/members/{userId}` _(AdminOnly)_
Remove a member from the tenant.

- Returns `400` if the caller tries to remove themselves

---

### Invitations

#### `POST /api/tenants/{id}/invite` _(AdminOnly)_
Invite a user by email. Sends an invitation email; **does not return the token**.

Body: `{ "email": "string", "role": "Viewer|Editor|Admin|Approver" }`

Response `201`:
```json
{ "id": "uuid", "tenantId": "uuid", "email": "user@example.com", "role": "Viewer", "expiresAt": "..." }
```

- Normalizes email to lowercase
- Returns `409 Conflict` if the email already belongs to a tenant member or has an active pending invite
- Returns `400` for invalid role strings
- Email delivery failure does **not** fail the request (invite is still created; share the accept URL manually if needed)

#### `GET /api/tenants/{id}/invite/preview` _(public — no auth required)_
Returns non-secret invite metadata for the accept page.

Query: `?token=<invite_token>`

Response `200`:
```json
{ "email": "user@example.com", "tenantName": "Acme Corp", "role": "Viewer", "expiresAt": "..." }
```

- Returns `404` if the token is invalid, expired, or already accepted

#### `POST /api/tenants/{id}/invite/accept` _(public — no auth required)_
Accept an invite, create the user account, and return a JWT for immediate login.

Body: `{ "token": "string", "password": "string" }`

Response `200`:
```json
{
  "token": "<jwt>",
  "user": { "id": "uuid", "email": "user@example.com", "displayName": "user", "role": "Viewer" }
}
```

- Password minimum: **8 characters**
- Returns `400` for expired, already-accepted, or wrong-tenant tokens
- The JWT can be stored and used immediately — no separate login step required

#### `GET /api/tenants/{id}/invites` _(AdminOnly)_
List pending (not yet accepted) invites for the tenant.

Response: array of invite objects (same shape as `POST /invite` response, excluding token).

#### `DELETE /api/tenants/{id}/invites/{inviteId}` _(AdminOnly)_
Revoke (delete) a pending invite. Returns `204 No Content`.

### `GET /api/tenants/{id}/config` _(ViewerOrAbove)_
Get tenant configuration.

Response:
```json
{
  "displayName": "Acme Corp",
  "logoUrl": null,
  "maxConcurrentExecutions": 10,
  "executionTimeoutSeconds": 3600,
  "defaultTimezone": "UTC",
  "allowGuestFormFill": true
}
```

### `PUT /api/tenants/{id}/config` _(AdminOnly)_
Update tenant configuration. Send only the fields you want to change — null fields are ignored.

Body:
```json
{
  "displayName": "Acme Corp",
  "maxConcurrentExecutions": 5,
  "executionTimeoutSeconds": 1800,
  "defaultTimezone": "America/New_York",
  "allowGuestFormFill": false
}
```

| Field | Default | Description |
|---|---|---|
| `displayName` | null | Branded name shown in UI |
| `logoUrl` | null | HTTPS URL to a logo image |
| `maxConcurrentExecutions` | 10 | Max queued+running executions; 0 = unlimited |
| `executionTimeoutSeconds` | 3600 | Max seconds per execution; 0 = unlimited |
| `defaultTimezone` | `"UTC"` | IANA timezone for cron/timestamp display |
| `allowGuestFormFill` | true | When false, form fill page requires auth |

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

### GET /api/settings/ai-status
Returns AI provider readiness for the current tenant without exposing API keys.

**Response:**
```json
{
  "defaultProvider": "openai",
  "defaultModel": "gpt-4o-mini",
  "isDefaultConfigured": true,
  "providers": {
    "openai":    true,
    "anthropic": false,
    "azure":     false,
    "ollama":    true
  }
}
```
Useful for disabling AI features in the UI when the provider is not configured.

---

## 11. AI Chat History

### `GET /api/ai/sessions`
Lists AI chat sessions for the current tenant (workflow designer assist, form generator).

**Query params:** `surface` (filter: `workflow-assist` | `form-generator`), `contextId` (Guid), `page`, `pageSize` (max 50)

**Response:**
```json
[
  {
    "id": "uuid",
    "surface": "workflow-assist",
    "contextId": null,
    "createdAt": "2026-05-30T18:00:00Z",
    "updatedAt": "2026-05-30T18:01:00Z"
  }
]
```

### `GET /api/ai/sessions/{sessionId}/messages`
Returns all messages in a session ordered by time.

**Response:**
```json
[
  {
    "id": "uuid",
    "role": "user",
    "contentText": "Create a workflow that sends an email",
    "toolName": null,
    "toolInputJson": null,
    "toolOutputJson": null,
    "promptTokens": 0,
    "completionTokens": 0,
    "totalTokens": 0,
    "model": null,
    "provider": null,
    "createdAt": "2026-05-30T18:00:00Z"
  },
  {
    "id": "uuid",
    "role": "assistant",
    "contentText": "I've created a workflow...",
    "promptTokens": 1200,
    "completionTokens": 450,
    "totalTokens": 1650,
    "model": "gpt-4o-mini",
    "provider": "openai",
    "createdAt": "2026-05-30T18:00:05Z"
  }
]
```

### `GET /api/ai/usage-summary`
Returns token usage summary for the tenant.

**Response:**
```json
{
  "totalSessions": 12,
  "totalTokens": 45230,
  "bySurface": [
    { "surface": "workflow-assist", "sessionCount": 8 },
    { "surface": "form-generator",  "sessionCount": 4 }
  ]
}
```

---

## 12. Secrets

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

All list endpoints (`/api/workflows`, `/api/executions`, `/api/forms`, `/api/approvals`) return a standard paged envelope:

```json
{ "items": [ "..." ], "page": 1, "pageSize": 20, "total": 137 }
```

Default page size: **20**. Pass `?page=2&pageSize=50` to paginate.

| Endpoint | Filter params |
|---|---|
| `GET /api/workflows` | `?search=` (name) |
| `GET /api/executions` | `?status=`, `?search=` (correlationId) |
| `GET /api/forms` | `?search=` (name or description) |
| `GET /api/approvals` | — |

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
  ],
  "provider": "openai",
  "model": "gpt-4o-mini",
  "totalTokens": 1842
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
GET /api/forms?search=&page=1&pageSize=20
Authorization: Bearer <token>
```

Response `200`: `PagedResponse<FormResponse>` — see §14 Pagination.

Query params:
- `search` — case-insensitive contains match on name or description (optional)
- `page` / `pageSize` — default 1 / 20

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

Field types: `text` | `number` | `select` | `date` | `email` | `boolean` | `file`

**File field** — uploads to `POST /api/documents/upload` when the user selects a file. The submitted value is `{ "id": "uuid", "filename": "invoice.pdf", "mimeType": "application/pdf" }` instead of a plain string. Optional `accept` restricts choosable types:

```json
{ "key": "invoice", "label": "Invoice PDF", "type": "file", "required": true, "accept": ".pdf,application/pdf" }
```

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

> **Validation:** For `system.data-checkpoint` tokens, the endpoint validates the `fields` config before consuming the token. On validation failure (400), the token is **not consumed** � the caller can fix the payload and POST again with the same URL.

**400 response (validation failure):**
```json
{
  "error": "Validation failed. Token not consumed � fix the payload and retry.",
  "errors": ["Missing required field: 'name'", "Field 'amount' must be a number (got 'abc')"]
}
```

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


---

## 18. Playground

### POST /api/playground/seed _(EditorOrAbove)_
Seeds (or upserts) the sample "User Onboarding" workflow under the current tenant.
Idempotent � safe to call multiple times.

Creates three forms (pg-personal-info, pg-employment, pg-preferences) if they don't exist, then creates/re-creates the workflow with a fresh activated version linking them in sequence.

Response:
`json
{ "workflowId": "uuid", "message": "Playground seeded successfully." }
`

Used by the /playground page in the frontend UI.
### POST /api/playground/seed-external _(EditorOrAbove)_
Seeds (or upserts) the "External Data Intake" workflow. Creates a workflow with 2 `system.data-checkpoint` nodes and 2 `data.db-execute` nodes. No forms required — external systems POST data to the resume URLs.

Accepts an optional JSON body to configure the database nodes:
```json
{
  "customer": { "connectionString": "Host=...;Database=...;Username=...;Password=...", "statement": "INSERT INTO customers (name, email) VALUES (@name, @email)" },
  "order":    { "connectionString": "...", "statement": "INSERT INTO orders (items, amount) VALUES (@items, @amount)" }
}
```
If omitted (or "Skip DB setup" in the UI), db nodes are seeded with empty config.

Checkpoint field validation is pre-configured:
- **Customer** checkpoint expects `name` (string, required) + `email` (string, required)
- **Order** checkpoint expects `items` (string, required) + `amount` (number, required)

On validation failure at the resume endpoint, the token is **not consumed** — the external system can fix the payload and retry with the same URL.

Response: `{ "workflowId": "uuid", "message": "External playground seeded successfully." }`
