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
    { "id": "â€¦", "name": "Contract Review", "activeVersion": 3, "updatedAt": "â€¦" }
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
  "definition": { /* see ARCHITECTURE.md Â§6 */ }
}
```

201 with `{ id, activeVersion }`.

### `GET /api/workflows/{workflowId}`
Get the workflow with its active version definition.

### `PUT /api/workflows/{workflowId}`
Update workflow metadata (name/description) â€” does **not** modify versions.

### `DELETE /api/workflows/{workflowId}`
Soft-delete a workflow.

### `POST /api/workflows/{workflowId}/versions`
Create a new version (drafts a new definition).

Body: `{ "definition": { â€¦ } }`

Response: `{ "id": "â€¦", "versionNumber": 2 }`

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
  "startedAt": "â€¦",
  "completedAt": null,
  "triggeredBy": "uuid",
  "input": { â€¦ },
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
      "startedAt": "â€¦",
      "completedAt": "â€¦",
      "input": { â€¦ },
      "output": { â€¦ },
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
- `file` â€” required
- `meta` â€” optional JSON string

Response:
```json
{
  "id": "uuid",
  "filename": "contract.pdf",
  "mimeType": "application/pdf",
  "sizeBytes": 123456,
  "sha256": "â€¦"
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
      "description": "â€¦",
      "category": "documents",
      "version": "0.1.0",
      "iconKey": "file-pdf",
      "inputs":  [ { "key": "document", "type": "DocumentRef", "required": true, â€¦ } ],
      "outputs": [ { "key": "text", "type": "String" }, â€¦ ],
      "configuration": [ { "key": "ocrFallback", "type": "Boolean", "defaultValue": false } ]
    }
  ]
}
```

The frontend uses this to render the node palette and configuration drawer.

---

## 6. Health & Meta

### `GET /api/health`
Liveness + dependency probes.

### `GET /api/version`
Service build version.

---

## 7. Auth (MVP)

MVP ships with email/password JWT auth. Endpoints:

- `POST /api/auth/login` â†’ `{ token, user }`
- `POST /api/auth/logout`
- `GET /api/auth/me`

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

## 9. Idempotency

Mutating endpoints (`POST /api/workflows/{id}/execute`, approvals) accept an optional header:

```
Idempotency-Key: <uuid>
```

If the same key is replayed within 24h, the original response is returned without re-executing.

---

## 10. Pagination

Standard envelope:

```json
{ "items": [ â€¦ ], "page": 1, "pageSize": 20, "total": 137 }
```

Default page size 20, max 100.

---

## 11. Gmail Credentials

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/gmail/auth/start | JWT | Start OAuth2 flow; redirects to Google consent screen |
| GET | /api/gmail/callback | Public | OAuth2 callback; exchanges code for tokens and stores credential |
| GET | /api/gmail/credentials | JWT | List saved credentials (name, email — no secrets) |
| DELETE | /api/gmail/credentials/{id} | JWT | Delete a credential |

### Start OAuth flow

GET /api/gmail/auth/start?name=my-gmail&clientId=...&clientSecret=...

Redirects browser to Google. After consent, Google calls /api/gmail/callback which stores the credential.

### Use credential in GmailReadNode

Set credentialName: "my-gmail" in the node config instead of providing clientId/clientSecret/efreshToken inline.

### Settings

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/settings | JWT | Get platform settings (keys masked) |
| PUT | /api/settings | JWT | Update settings; empty values ignored |
| POST | /api/settings/test/openai | JWT | Test OpenAI connection |
