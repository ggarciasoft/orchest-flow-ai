# Rules: Observability

1. Every inbound API request is assigned a `CorrelationId` (or uses the inbound `X-Correlation-Id` header). This id propagates to queue messages, worker, and every LLM call.

2. `CorrelationId` is stored on `workflow_executions.correlation_id` and included in all related log entries.

3. All logs are **structured** (Serilog JSON). No bare string messages without accompanying fields.

4. Required fields on execution-related log entries:
   - `correlationId`
   - `tenantId`
   - `executionId` (when relevant)
   - `nodeExecutionId` (when relevant)
   - `nodeType` (when relevant)
   - `service` (`api` | `worker` | `ai`)
   - `version` (build SHA or semver)

5. Required metrics (emit from day one):
   - `orchestai_workflow_executions_started_total`
   - `orchestai_workflow_executions_duration_seconds`
   - `orchestai_node_executions_total`
   - `orchestai_node_execution_duration_seconds`
   - `orchestai_node_retries_total`
   - `orchestai_approvals_pending` (gauge)
   - `orchestai_llm_calls_total`
   - `orchestai_llm_tokens_total`
   - `orchestai_llm_cost_usd_total`
   - `orchestai_http_requests_total`

6. OpenTelemetry spans are required for: `http.request`, `workflow.execute`, `node.execute`, `llm.call`. W3C tracecontext propagates over queue message headers.

7. The **execution timeline** is the authoritative debugging surface. It must always be complete — every node execution must be persisted before the worker advances.

8. AI usage (`provider`, `model`, `prompt_version`, `tokens`, `cost`) is logged to `ai_usage_logs` on every LLM call. Missing entries are a bug.

9. Full prompt/response payloads are **not** stored in application logs. They may be stored to object storage (configurable per tenant) with retention policies.

10. Sensitive field values are redacted before logging. Log the field name (e.g. `"api_key": "[REDACTED]"`), never the value.
