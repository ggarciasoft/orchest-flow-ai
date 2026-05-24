# Local Development Setup

This document gets a new developer from clone → running demo in under 10 minutes.

---

## 1. Prerequisites

- **Git**
- **Docker Desktop** (with Compose v2)
- **.NET SDK 9.0+**
- **Node.js 20+** (LTS) and **pnpm 9+**
- **PostgreSQL client** (`psql`) — optional but handy
- An **OpenAI API key** (or Azure OpenAI / Anthropic key)

> Windows users: PowerShell 7+ is recommended. On macOS/Linux, any modern shell works.

---

## 2. First-Time Setup

```bash
# 1. Clone
git clone https://github.com/<org>/orchestai.git
cd orchestai

# 2. Copy environment template
cp .env.example .env
# Edit .env: at minimum, set ORCHESTAI_LLM__OPENAI__API_KEY=...

# 3. Bring up infrastructure (Postgres, Redis)
docker compose up -d postgres redis

# 4. Apply migrations
dotnet run --project services/OrchestAI.Api -- db update

# 5. Restore + build backend
dotnet build OrchestAI.sln

# 6. Install + build frontend
pnpm install --frozen-lockfile
pnpm --filter @orchestai/web build
```

---

## 3. Running the Stack

Open three terminals (or use the provided `Procfile` / `tmuxinator` config):

```bash
# Terminal A — API
dotnet run --project services/OrchestAI.Api

# Terminal B — Worker
dotnet run --project services/OrchestAI.Worker

# Terminal C — Frontend
pnpm --filter @orchestai/web dev
```

Now open:

- API:        `http://localhost:5080`
- Frontend:   `http://localhost:3000`
- Postgres:   `localhost:5432` (user `orchestai`, db `orchestai`)
- Redis:      `localhost:6379`

---

## 4. Docker Compose

The compose setup is split into three files for flexibility:

| File | Purpose |
|---|---|
| `docker-compose.yml` | Infrastructure only: PostgreSQL + Redis |
| `docker-compose.app.yml` | Application: API + Worker + Web frontend |
| `docker-compose.observability.yml` | Monitoring: OTEL Collector + Prometheus + Grafana |

### Infrastructure only (DB + Redis)
```bash
docker compose up -d
```

### Full stack (infra + app)
```bash
docker compose -f docker-compose.yml -f docker-compose.app.yml up -d --build
```

### Full stack with observability
```bash
docker compose -f docker-compose.yml -f docker-compose.app.yml -f docker-compose.observability.yml up -d --build
```

> The API auto-applies EF Core migrations on startup — no manual `dotnet ef database update` needed.

### Service URLs
| Service | URL |
|---|---|
| API + Swagger | http://localhost:5080/swagger |
| Frontend | http://localhost:3000 |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3001 (admin/admin) |

---

## 5. Seed Data

```bash
dotnet run --project services/OrchestAI.Api -- seed demo
```

Creates:
- A demo tenant
- A demo admin user (`demo@orchestai.local` / `password`)
- The Contract Review workflow at version 1 (active)
- A sample contract PDF in `samples/contract-review-workflow/sample-contract.pdf`

---

## 6. Running the Demo End-to-End

1. Open `http://localhost:3000` and log in as the demo user.
2. Navigate to **Documents** → upload `samples/contract-review-workflow/sample-contract.pdf`.
3. Go to **Workflows** → open **Contract Review Workflow** → click **Execute**.
4. Pick the uploaded document → **Run**.
5. Watch the **Execution Details** page for the timeline.
6. When risk is High, the execution pauses. Go to **Approvals**, review, and approve.
7. Execution completes and the timeline shows the full path.

---

## 7. Environment Variables

```env
# Database
ORCHESTAI_DB__CONNECTION_STRING=Host=localhost;Database=orchestai;Username=orchestai;Password=orchestai

# Redis (optional in MVP)
ORCHESTAI_REDIS__CONNECTION_STRING=localhost:6379

# AI
ORCHESTAI_LLM__DEFAULT_PROVIDER=openai
ORCHESTAI_LLM__DEFAULT_MODEL=gpt-4o-mini
ORCHESTAI_LLM__OPENAI__API_KEY=sk-...
ORCHESTAI_LLM__AZURE__ENDPOINT=
ORCHESTAI_LLM__AZURE__API_KEY=
ORCHESTAI_LLM__ANTHROPIC__API_KEY=
ORCHESTAI_LLM__FALLBACKS=

# Auth
ORCHESTAI_AUTH__JWT_SIGNING_KEY=devdevdevdevdevdevdevdev    # 32+ bytes
ORCHESTAI_AUTH__JWT_ISSUER=orchestai
ORCHESTAI_AUTH__JWT_AUDIENCE=orchestai-web

# Storage
ORCHESTAI_STORAGE__ROOT=./data/uploads

# Web
NEXT_PUBLIC_API_BASE_URL=http://localhost:5080
```

---

## 8. Running Tests

```bash
# Backend
dotnet test OrchestAI.sln

# Frontend
pnpm --filter @orchestai/web test
pnpm --filter @orchestai/web test:e2e   # Playwright (requires the stack running)
```

---

## 9. Common Tasks

| Task                                    | Command                                                                       |
| --------------------------------------- | ----------------------------------------------------------------------------- |
| Add a database migration                | `dotnet ef migrations add <Name> --project OrchestAI.Infrastructure`         |
| Update DB                               | `dotnet run --project services/OrchestAI.Api -- db update`                    |
| Regenerate OpenAPI client (FE)          | `pnpm --filter @orchestai/web codegen`                                        |
| Format / lint                           | `dotnet format` · `pnpm lint` · `pnpm format`                                 |
| Seed demo data                          | `dotnet run --project services/OrchestAI.Api -- seed demo`                    |
| Reset local DB                          | `docker compose down -v postgres && docker compose up -d postgres && dotnet run --project services/OrchestAI.Api -- db update && dotnet run --project services/OrchestAI.Api -- seed demo` |

---

## 10. Troubleshooting

- **Postgres "role does not exist"** → recreate the Docker volume (`docker compose down -v postgres`).
- **`401` from `/api/auth/login`** → verify `ORCHESTAI_AUTH__JWT_SIGNING_KEY` is at least 32 bytes and matches across services.
- **AI calls fail with `401`** → check `ORCHESTAI_LLM__OPENAI__API_KEY`; logs will redact the key value.
- **Worker not picking up executions** → confirm Redis is running (or that the Postgres-only queue path is enabled in MVP).
- **Frontend can't reach API** → check `NEXT_PUBLIC_API_BASE_URL` and CORS allowlist in API config.

---

## 11. IDE Setup

- **VS Code:** install the C# Dev Kit, ESLint, Prettier, Tailwind CSS extensions.
- **Rider:** open `OrchestAI.sln` for backend; open `apps/web` separately or via the JS plugin.
- Recommended `.editorconfig` is checked in; honor it.
