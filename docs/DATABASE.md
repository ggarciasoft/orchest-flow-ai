# Database

PostgreSQL is the system of record. Redis (optional in MVP) is used for queueing and caching.

This document describes the MVP schema. Indexes, constraints, and migrations are owned by `OrchestFlowAI.Infrastructure`.

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
| tenant_id   | uuid FK     | â†’ tenants.id                                |
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
| created_by  | uuid FK     | â†’ users.id                  |
| created_at  | timestamptz |                             |
| updated_at  | timestamptz |                             |
| is_deleted  | boolean     | default false               |
| deleted_at  | timestamptz |                             |

Index: `(tenant_id, is_deleted)`.

### `workflow_versions`

| Column           | Type        | Notes                                  |
| ---------------- | ----------- | -------------------------------------- |
| id               | uuid PK     |                                        |
| workflow_id      | uuid FK     | â†’ workflows.id                         |
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
| triggered_by          | uuid FK     | â†’ users.id (nullable for system triggers)            |
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
| owner_id    | uuid FK     | â†’ users.id                             |
| filename    | text        |                                        |
| mime_type   | text        |                                        |
| size_bytes  | bigint      |                                        |
| storage_uri | text        | e.g. `tenant/<id>/uploads/<uuid>.pdf`  |
| sha256      | text        |                                        |
| created_at  | timestamptz |                                        |

### `gmail_credentials`

Stores OAuth2 credentials for Gmail integrations, scoped per tenant.

| Column         | Type         | Notes                                              |
| -------------- | ------------ | -------------------------------------------------- |
| id             | uuid PK      |                                                    |
| tenant_id      | uuid FK      | → tenants.id; indexed                              |
| name           | varchar(200) | Friendly reference name; unique per tenant         |
| client_id      | varchar(500) | Google OAuth2 client ID                            |
| client_secret  | varchar(500) | Google OAuth2 client secret                        |
| refresh_token  | text         | Long-lived refresh token with Gmail read scope     |
| email          | varchar(320) | Gmail address (populated after OAuth callback)     |
| created_at     | timestamptz  |                                                    |
| updated_at     | timestamptz  |                                                    |

Unique index: `(tenant_id, name)`.

### `platform_settings`

Stores per-tenant runtime-configurable key-value pairs (e.g. LLM API keys, default model). Hot-reloaded without restart via `OpenAIApiKeyHolder`.

| Column     | Type         | Notes                                             |
| ---------- | ------------ | ------------------------------------------------- |
| id         | uuid PK      |                                                   |
| tenant_id  | uuid FK      | → tenants.id                                      |
| key        | varchar(200) | Setting key (e.g. `llm.openai.apiKey`)            |
| value      | text         | Setting value                                     |
| updated_at | timestamptz  | Last updated                                      |

Unique index: `(tenant_id, key)`.

### `secrets`

Stores per-tenant encrypted named values for the Secret Vault.

| Column          | Type         | Notes                                              |
| --------------- | ------------ | -------------------------------------------------- |
| id              | uuid PK      |                                                    |
| tenant_id       | uuid FK      | → tenants.id; tenant scope                         |
| name            | varchar(200) | Friendly name (e.g. `openai-key`)                  |
| encrypted_value | text         | AES-256-CBC encrypted; **never returned to clients** |
| created_at      | timestamptz  |                                                    |
| updated_at      | timestamptz  |                                                    |

Unique index: `(tenant_id, name)`.

### `node_presets`

Tenant-scoped saved config snapshots for node types.

| Column      | Type         | Notes                                              |
| ----------- | ------------ | -------------------------------------------------- |
| id          | uuid PK      |                                                    |
| tenant_id   | uuid FK      | → tenants.id                                       |
| name        | varchar(200) | Display name                                       |
| node_type   | text         | Node type key (e.g. `integrations.http`)           |
| config_json | jsonb        | Saved configuration values                         |
| created_by  | uuid FK      | → users.id                                         |
| created_at  | timestamptz  |                                                    |
| updated_at  | timestamptz  |                                                    |

Index: `(tenant_id, node_type)`.

### `execution_queue`

Durable queue for workflow execution messages (used when Redis is unavailable).

| Column        | Type        | Notes                                              |
| ------------- | ----------- | -------------------------------------------------- |
| id            | uuid PK     |                                                    |
| tenant_id     | uuid FK     |                                                    |
| execution_id  | uuid FK     | → workflow_executions.id                           |
| message_type  | text        | `run` \| `resume` \| `cancel`                      |
| payload_json  | jsonb       | Message-specific data                              |
| visible_after | timestamptz | For delayed/retry scheduling                       |
| locked_until  | timestamptz | Worker lease lock expiry                           |
| attempt_count | int         | default 0                                          |
| created_at    | timestamptz |                                                    |

Index: `(visible_after, locked_until)` for worker polling.

### `tenant_invites`

Pending user invitations scoped per tenant.

| Column      | Type         | Notes                                              |
| ----------- | ------------ | -------------------------------------------------- |
| id          | uuid PK      |                                                    |
| tenant_id   | uuid FK      | → tenants.id                                       |
| email       | citext       | Invitee email address                              |
| role        | text         | Role to assign on accept                           |
| token       | text         | Unique invite token (32-char hex GUID)             |
| invited_by  | uuid FK      | → users.id                                         |
| expires_at  | timestamptz  | 24h from creation                                  |
| accepted_at | timestamptz  | Null until accepted                                |
| created_at  | timestamptz  |                                                    |

Unique index: `(tenant_id, token)`.

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
| action      | text        | `workflow.created`, `execution.started`, â€¦           |
| target_type | text        |                                                      |
| target_id   | uuid        |                                                      |
| payload_json| jsonb       |                                                      |
| created_at  | timestamptz |                                                      |

---

## 3. Optional Tables (post-MVP)

- `tenant_settings` â€” per-tenant config (default model, providers, retention).
- `prompts` â€” prompt template storage with versions (currently in code).
- `knowledge_bases`, `kb_documents`, `kb_chunks`, `kb_embeddings` â€” for RAG.
- `integrations` + `integration_credentials` â€” connector credentials.
- `agents` â€” agent configurations.

---

## 4. Migrations

- Migrations live in `OrchestFlowAI.Infrastructure/Migrations/`.
- EF Core migrations are used. The Infrastructure project is both the migrations project and its own startup project for `dotnet ef` commands.
- Migrations are forward-only in production; backfills are explicit data scripts.
- Auto-migration runs on API startup with 5 retries (fatal if all retries fail â€” app will not start with missing tables).
- To add a migration:
  ```sh
  dotnet ef migrations add <Name> \
    --project packages/OrchestFlowAI.Infrastructure \
    --startup-project packages/OrchestFlowAI.Infrastructure
  ```
- To apply manually (dev):
  ```sh
  dotnet ef database update \
    --project packages/OrchestFlowAI.Infrastructure \
    --startup-project packages/OrchestFlowAI.Infrastructure \
    --connection "Host=localhost;Database=OrchestFlowAI;Username=OrchestFlowAI;Password=<pwd>"
  ```

---

## 5. Indexing Strategy

Primary access patterns to optimize for in MVP:

- "Show executions for workflow X" â†’ `(workflow_id, started_at desc)`
- "Show pending approvals for tenant T" â†’ `(tenant_id, status, requested_at)`
- "Show timeline for execution E" â†’ `(workflow_execution_id, step)`
- "Recent AI usage for tenant T" â†’ `(tenant_id, created_at desc)`

Add indexes only after access patterns are confirmed; avoid speculative ones.

---

## 6. Data Retention

MVP defaults:

- Document blobs retained until the workflow execution is hard-deleted.
- Execution rows retained indefinitely.
- AI usage logs retained 12 months (configurable).
- Audit logs retained indefinitely.

Post-MVP: per-tenant retention policy + automated purge job.

### PlatformSettings

Stores per-tenant runtime-configurable key-value pairs (e.g. LLM API keys, default model).

| Column | Type | Notes |
|--------|------|-------|
| Id | UUID | PK |
| TenantId | UUID | FK → Tenants |
| Key | VARCHAR(200) | Setting key (e.g. llm.openai.apiKey) |
| Value | TEXT | Setting value |
| UpdatedAt | TIMESTAMPTZ | Last updated |

Unique index: (TenantId, Key).

## Secrets

Stores per-tenant encrypted named values for the Secret Vault.

| Column | Type | Notes |
|--------|------|-------|
| Id | UUID | PK |
| TenantId | UUID | Tenant scope |
| Name | VARCHAR(200) | Friendly name (e.g. "openai-key") |
| EncryptedValue | TEXT | AES-256-CBC encrypted; never returned to clients |
| CreatedAt | TIMESTAMPTZ | Created timestamp |
| UpdatedAt | TIMESTAMPTZ | Last updated |

Unique index: (TenantId, Name).
