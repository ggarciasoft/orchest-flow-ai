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
Update workflow metadata (name/description) — does **not** modify versions.

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
