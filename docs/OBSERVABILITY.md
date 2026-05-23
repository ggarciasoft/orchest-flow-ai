# Observability

OrchestAI is built to be debuggable in production. Every execution must be traceable end-to-end.

---

## 1. Pillars

| Pillar  | MVP                                             | Post-MVP                                      |
| ------- | ----------------------------------------------- | --------------------------------------------- |
| Logs    | Structured (Serilog) with correlation IDs        | Centralized aggregation (Loki/ELK)            |
| Traces  | OpenTelemetry spans across Api → Worker → AI     | Distributed tracing (Jaeger/Tempo) dashboards |
| Metrics | Prometheus-compatible counters & histograms      | Grafana dashboards + alerting                 |
| AI cost | `ai_usage_logs` table + summary in dashboard     | Cost dashboard, anomaly detection             |
| Audit   | `audit_logs` table for security events           | SIEM export                                   |

---

## 2. Correlation

- Every API request is assigned a `CorrelationId` (or uses the inbound `X-Correlation-Id` header).
- `CorrelationId` propagates to the queue message, the worker, and into every LLM call as `metadata.correlation_id`.
- `WorkflowExecution.correlation_id` is stored alongside the row.
- Logs include `correlationId`, `tenantId`, `executionId`, `nodeExecutionId`, `nodeType`.

---

## 3. Logging

- Library: Serilog (.NET) with JSON sinks.
- Console sink for local dev; file or external sink in deployments.
- Log levels:
  - `Trace` — verbose engine internals (off by default)
  - `Debug` — node-by-node decisions
  - `Information` — execution lifecycle, approval transitions
  - `Warning` — retryable errors, rate limits
  - `Error` — node failures, validation errors
  - `Critical` — engine bugs, persistence failures

### Required Fields per Log Entry

- `timestamp`, `level`, `message`
- `correlationId`, `tenantId`
- `executionId` and `nodeExecutionId` when relevant
- `service` (`api` | `worker` | `ai`)
- `version` (build SHA)

---

## 4. Tracing

- OpenTelemetry spans:
  - `http.request` (Api)
  - `workflow.execute` (Worker, spans the whole execution)
  - `node.execute` (per node)
  - `llm.call` (per LLM request)
  - `db.query` (significant queries)
- Span attributes mirror logging fields.
- Trace context propagates over the queue (W3C tracecontext on message headers).

---

## 5. Metrics

Counters / histograms (Prometheus naming):

- `orchestai_workflow_executions_started_total{tenant,workflow,status}`
- `orchestai_workflow_executions_duration_seconds{tenant,workflow,status}`
- `orchestai_node_executions_total{node_type,status}`
- `orchestai_node_execution_duration_seconds{node_type}`
- `orchestai_node_retries_total{node_type}`
- `orchestai_approvals_pending` (gauge)
- `orchestai_llm_calls_total{provider,model,outcome}`
- `orchestai_llm_tokens_total{provider,model,type}` (type=`prompt|completion`)
- `orchestai_llm_cost_usd_total{provider,model}`
- `orchestai_http_requests_total{method,route,status}`

---

## 6. Execution Timeline

The user-facing timeline is a first-class observability surface. For each node execution it shows:

- Status, start/end times, duration
- Inputs (truncated, with download link for full payload)
- Outputs (truncated)
- Retry attempts
- AI usage when present
- Errors with stack-free human messages + an error code

The same data backs `GET /api/executions/{id}/timeline`.

---

## 7. AI Usage Tracking

Every LLM call writes to `ai_usage_logs`. Aggregations:

- Per workflow execution (in the timeline)
- Per workflow (in workflow detail)
- Per tenant (in dashboard)
- Per model/provider (in admin settings)

Cost estimation uses provider rate tables defined in `OrchestAI.AI` and updated periodically.

---

## 8. Alerting (Post-MVP)

Suggested initial alerts:

- Worker failed to pick up jobs for > 5 minutes.
- Workflow failure rate per tenant > X% over 15 minutes.
- Provider error rate spike (e.g. > 10% over 5 minutes).
- Daily AI cost crossing per-tenant threshold.
- Queue depth growing beyond a configured limit.

---

## 9. Local Dev UX

- All services log to console with colorized formatter.
- A development dashboard route (`/dev/observability`) shows live counters via the metrics endpoint (admin only).
- The `docker-compose` profile `with-otel` brings up an OpenTelemetry collector + Grafana + Tempo + Prometheus for full local tracing.

---

## 10. Privacy Considerations

- Logs avoid full document text or full LLM responses by default.
- Truncation thresholds are configurable per tenant.
- Sensitive fields are redacted before logging.
- Stored prompt/response payloads (in `node_executions.input_json/output_json`) respect the same redaction rules and retention windows.
