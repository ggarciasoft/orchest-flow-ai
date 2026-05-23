# Database

PostgreSQL is the system of record. Redis (optional in MVP) is used for queueing and caching.

This document describes the MVP schema. Indexes, constraints, and migrations are owned by `OrchestAI.Infrastructure`.

---

## 1. Conventions

- Primary keys are `uuid` (v7 preferred for time-ordered indexing).
- All tables include `tenant_id`, `created_at`, `updated_at` where applicable.
- Soft delete via `is_deleted` + `deleted_at` on user-facing entities.
- JSON columns are `jsonb`.
- Timestamps are `timestamptz` (UTC).
- Naming: `snake_case` tables and columns.

---

## 2. Tables (MVP)

### `tenants`

| Column      | Type        | Notes                |
| ----------- | ----------- | -------------------- |
| id          | uuid PK     |                      |
| name        | text        |                      |
| created_at  | timestamptz | default now()        |

### `users`

| Column      | Type        | Notes                                       |
| ----------- | ----------- | ------------------------------------------- |
| id          | uuid PK     |                                             |
| tenant_id   | uuid FK     | → tenants.id                                |
| email       | citext      | unique within tenant                        |
| display_name| text        |                                             |
| role        | text        | `admin` \| `editor` \| `approver` \| `viewer` |
| created_at  | timestamptz |                                             |

### `workflows`

| Column      | Type        | Notes                       |
| ----------- | ----------- | --------------------------- |
| id          | uuid PK     |                             |
| tenant_id   | uuid FK     |                             |
| name        | text        |                             |
| description | text        |                             |
| created_by  | uuid FK     | → users.id                  |
| created_at  | timestamptz |                             |
| updated_at  | timestamptz |                             |
| is_deleted  | boolean     | default false               |
| deleted_at  | timestamptz |                             |

Index: `(tenant_id, is_deleted)`.

### `workflow_versions`

| Column           | Type        | Notes                                  |
| ---------------- | ----------- | -------------------------------------- |
| id               | uuid PK     |                                        |
| workflow_id      | uuid FK     | → workflows.id                         |
| version_number   | int         |                                        |
| definition_json  | jsonb       | canonical workflow definition          |
| is_active        | boolean     | only one active per workflow           |
| created_by       | uuid FK     |                                        |
| created_at       | timestamptz |                                        |

Constraints: unique `(workflow_id, version_number)`. Partial unique on `is_active=true`.

### `workflow_executions`

| Column                | Type        | Notes                                                |
| --------------------- | ----------- | ---------------------------------------------------- |
| id                    | uuid PK     |                                                      |
| tenant_id             | uuid FK     |                                                      |
| workflow_id           | uuid FK     |                                                      |
| workflow_version_id   | uuid FK     |                                                      |
| status                | text        | `Queued|Running|Paused|Completed|Failed|Cancelled` |
| started_at            | timestamptz |                                                      |
| completed_at          | timestamptz |                                                      |
| triggered_by          | uuid FK     | → users.id (nullable for system triggers)            |
| input_json            | jsonb       |                                                      |
| output_json           | jsonb       |                                                      |
| error_message         | text        |                                                      |
| correlation_id        | text        |                                                      |

Indexes: `(tenant_id, status, started_at desc)`, `(workflow_id, started_at desc)`.

### `node_executions`

| Column                  | Type        | Notes                                                  |
| ----------------------- | ----------- | ------------------------------------------------------ |
| id                      | uuid PK     |                                                        |
| workflow_execution_id   | uuid FK     |                                                        |
| node_id                 | text        | id from workflow JSON                                  |
| node_type               | text        | descriptor type                                        |
| status                  | text        | `Pending|Running|Succeeded|Failed|WaitingForApproval|Skipped|Cancelled` |
| started_at              | timestamptz |                                                        |
| completed_at            | timestamptz |                                                        |
| input_json              | jsonb       |                                                        |
| output_json             | jsonb       |                                                        |
| error_message           | text        |                                                        |
| retry_count             | int         | default 0                                              |
| step                    | int         | logical clock for ordering                             |

Index: `(workflow_execution_id, step)`.

### `approval_requests`

| Column                  | Type        | Notes                                              |
| ----------------------- | ----------- | -------------------------------------------------- |
| id                      | uuid PK     |                                                    |
| tenant_id               | uuid FK     |                                                    |
| workflow_execution_id   | uuid FK     |                                                    |
| node_execution_id       | uuid FK     |                                                    |
| status                  | text        | `Pending|Approved|Rejected|Expired`                |
| payload_json            | jsonb       | what the approver sees                             |
| assignees_json          | jsonb       | array of user/role refs                            |
| requested_at            | timestamptz |                                                    |
| responded_at            | timestamptz |                                                    |
| requested_by            | uuid FK     |                                                    |
| responded_by            | uuid FK     |                                                    |
| decision                | text        | `approved|rejected`                                |
| comment                 | text        |                                                    |
| sla_minutes             | int         |                                                    |

Indexes: `(tenant_id, status, requested_at desc)`.

### `documents`

| Column      | Type        | Notes                                  |
| ----------- | ----------- | -------------------------------------- |
| id          | uuid PK     |                                        |
| tenant_id   | uuid FK     |                                        |
| owner_id    | uuid FK     | → users.id                             |
| filename    | text        |                                        |
| mime_type   | text        |                                        |
| size_bytes  | bigint      |                                        |
| storage_uri | text        | e.g. `tenant/<id>/uploads/<uuid>.pdf`  |
| sha256      | text        |                                        |
| created_at  | timestamptz |                                        |

### `ai_usage_logs`

| Column                  | Type           | Notes                          |
| ----------------------- | -------------- | ------------------------------ |
| id                      | uuid PK        |                                |
| tenant_id               | uuid FK        |                                |
| workflow_execution_id   | uuid FK        |                                |
| node_execution_id       | uuid FK        |                                |
| provider                | text           |                                |
| model                   | text           |                                |
| prompt_version          | text           |                                |
| prompt_tokens           | int            |                                |
| completion_tokens       | int            |                                |
| total_tokens            | int            |                                |
| estimated_cost          | numeric(12,6)  |                                |
| created_at              | timestamptz    |                                |

Indexes: `(tenant_id, created_at desc)`, `(workflow_execution_id)`.

### `audit_logs`

| Column      | Type        | Notes                                                |
| ----------- | ----------- | ---------------------------------------------------- |
| id          | uuid PK     |                                                      |
| tenant_id   | uuid FK     |                                                      |
| actor_id    | uuid FK     | nullable for system actors                           |
| action      | text        | `workflow.created`, `execution.started`, …           |
| target_type | text        |                                                      |
| target_id   | uuid        |                                                      |
| payload_json| jsonb       |                                                      |
| created_at  | timestamptz |                                                      |

---

## 3. Optional Tables (post-MVP)

- `tenant_settings` — per-tenant config (default model, providers, retention).
- `prompts` — prompt template storage with versions (currently in code).
- `knowledge_bases`, `kb_documents`, `kb_chunks`, `kb_embeddings` — for RAG.
- `integrations` + `integration_credentials` — connector credentials.
- `agents` — agent configurations.

---

## 4. Migrations

- Migrations live in `OrchestAI.Infrastructure/Persistence/Migrations/`.
- Use EF Core migrations or a runner like FluentMigrator (decision recorded as ADR).
- Migrations are forward-only in production; backfills are explicit data scripts.

---

## 5. Indexing Strategy

Primary access patterns to optimize for in MVP:

- "Show executions for workflow X" → `(workflow_id, started_at desc)`
- "Show pending approvals for tenant T" → `(tenant_id, status, requested_at)`
- "Show timeline for execution E" → `(workflow_execution_id, step)`
- "Recent AI usage for tenant T" → `(tenant_id, created_at desc)`

Add indexes only after access patterns are confirmed; avoid speculative ones.

---

## 6. Data Retention

MVP defaults:

- Document blobs retained until the workflow execution is hard-deleted.
- Execution rows retained indefinitely.
- AI usage logs retained 12 months (configurable).
- Audit logs retained indefinitely.

Post-MVP: per-tenant retention policy + automated purge job.
