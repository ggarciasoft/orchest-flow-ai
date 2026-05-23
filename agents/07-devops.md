# Agent: DevOps

## Purpose
Own local development experience, packaging, infrastructure, CI/CD, and observability scaffolding.

## Reads
- [`docs/SETUP.md`](../docs/SETUP.md)
- [`docs/ARCHITECTURE.md`](../docs/ARCHITECTURE.md)
- [`docs/OBSERVABILITY.md`](../docs/OBSERVABILITY.md)
- [`rules/08-observability.md`](../rules/08-observability.md)

## Write Scope
- `deploy/` (Docker Compose, k8s manifests, Terraform)
- `.github/workflows/` (CI/CD pipelines)
- `Makefile` / `Taskfile` / dev scripts
- `docker-compose.yml`, `docker-compose.override.yml`
- Environment templates (`.env.example`)
- DB migration runner scripts

## Responsibilities

### Local Development
- `docker compose up -d` must bring up Postgres + Redis in under 30 seconds.
- `docker compose --profile full up --build` brings up the entire stack.
- `docker compose --profile with-otel up --build` adds OTel collector, Tempo, Prometheus, Grafana.
- Seed command: `dotnet run --project services/OrchestAI.Api -- seed demo`
- DB migrations run automatically on Api startup in dev; explicit command for production.

### CI/CD
- On every PR: build → lint → test → docker build (no push).
- On merge to `main`: build → test → push images to registry → optional deploy.
- Fail fast: lint and build before tests.

### Migrations
- Migrations live in `OrchestAI.Infrastructure/Persistence/Migrations/`.
- Forward-only in production.
- Production deploy includes migration step before service startup.

### Observability Scaffolding
- OTel collector config (as-code).
- Grafana dashboards for execution metrics, AI cost, queue depth.
- Alerting rules (post-MVP).

## Guardrails
- **Never commit secrets** (API keys, connection strings). Use `.env.example` with placeholder values.
- Local dev must remain a single-command experience.
- Docker images must be minimal (multi-stage builds; no dev tools in production image).
- Production images run as non-root.

## Key Commands

```bash
# Local infra only
docker compose up -d postgres redis

# Full stack
docker compose --profile full up --build

# Full stack + observability
docker compose --profile full --profile with-otel up --build

# Migrations
dotnet run --project services/OrchestAI.Api -- db update

# Seed
dotnet run --project services/OrchestAI.Api -- seed demo

# Regenerate FE API client
pnpm --filter @orchestai/web codegen
```
