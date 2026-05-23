# Rules: Database

1. Migrations are **forward-only** in production. Never write a down migration that would be run in production. Backfills are explicit data scripts, not migration rollbacks.

2. Every domain table that contains user data includes `tenant_id`. No exceptions.

3. All queries filter by `tenant_id` via the repository layer. Never rely on call-site discipline to add tenant filters.

4. Use `jsonb` for free-form payloads (workflow definitions, node inputs/outputs, approval payloads). Avoid storing structured data only in JSON columns — if you query a field, it should be a real column.

5. Primary keys are `uuid` (prefer v7 for time-ordered indexing).

6. All timestamps are `timestamptz` (UTC). Never store naive datetimes.

7. Naming convention: `snake_case` tables and columns.

8. Do not add indexes speculatively. Add them when a specific access pattern is confirmed and measured.

9. Required indexes (confirmed access patterns, add from day one):
   - `(tenant_id, is_deleted)` on `workflows`
   - `(workflow_id, started_at desc)` on `workflow_executions`
   - `(tenant_id, status, started_at desc)` on `workflow_executions`
   - `(workflow_execution_id, step)` on `node_executions`
   - `(tenant_id, status, requested_at desc)` on `approval_requests`
   - `(tenant_id, created_at desc)` on `ai_usage_logs`

10. Migrations are checked in to version control. Production never receives ad-hoc schema changes.

11. Schema changes require a migration. Never alter production tables directly.

12. Soft delete (`is_deleted`, `deleted_at`) on user-facing entities (`workflows`, `workflow_versions`). Hard deletes only for ephemeral/log data via retention policies.
