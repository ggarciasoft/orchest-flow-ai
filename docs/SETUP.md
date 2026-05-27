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
git clone https://github.com/<org>/OrchestFlowAI.git
cd OrchestFlowAI

# 2. Copy environment template
cp .env.example .env
# Edit .env: at minimum, set OrchestFlowAI_LLM__OPENAI__API_KEY=...

# 3. Bring up infrastructure (Postgres, Redis)
docker compose up -d postgres redis

# 4. Apply migrations
dotnet run --project services/OrchestFlowAI.Api -- db update

# 5. Restore + build backend
dotnet build OrchestFlowAI.sln

# 6. Install + build frontend
pnpm install --frozen-lockfile
pnpm --filter @OrchestFlowAI/web build
```

---

## 3. Running the Stack

Open three terminals (or use the provided `Procfile` / `tmuxinator` config):

```bash
# Terminal A — API
dotnet run --project services/OrchestFlowAI.Api

# Terminal B — Worker
dotnet run --project services/OrchestFlowAI.Worker

# Terminal C — Frontend
pnpm --filter @OrchestFlowAI/web dev
```

Now open:

- API:        `http://localhost:5080`
- Frontend:   `http://localhost:3000`
- Postgres:   `localhost:5432` (user `OrchestFlowAI`, db `OrchestFlowAI`)
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
dotnet run --project services/OrchestFlowAI.Api -- seed demo
```

Creates:
- A demo tenant
- A demo admin user (`demo@OrchestFlowAI.local` / `password`)
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
OrchestFlowAI_DB__CONNECTION_STRING=Host=localhost;Database=OrchestFlowAI;Username=OrchestFlowAI;Password=OrchestFlowAI

# Redis (optional in MVP)
OrchestFlowAI_REDIS__CONNECTION_STRING=localhost:6379

# AI
OrchestFlowAI_LLM__DEFAULT_PROVIDER=openai
OrchestFlowAI_LLM__DEFAULT_MODEL=gpt-4o-mini
OrchestFlowAI_LLM__OPENAI__API_KEY=sk-...   # Can also be set via Settings page (no restart needed)
OrchestFlowAI_LLM__AZURE__ENDPOINT=
OrchestFlowAI_LLM__AZURE__API_KEY=
OrchestFlowAI_LLM__ANTHROPIC__API_KEY=
OrchestFlowAI_LLM__FALLBACKS=

# Auth
OrchestFlowAI_AUTH__JWT_SIGNING_KEY=devdevdevdevdevdevdevdev    # 32+ bytes
OrchestFlowAI_AUTH__JWT_ISSUER=OrchestFlowAI
OrchestFlowAI_AUTH__JWT_AUDIENCE=OrchestFlowAI-web

# Secret Vault
ENCRYPTION_MASTER_KEY=dev-encryption-key-change-in-production   # CHANGE IN PRODUCTION

# Storage
OrchestFlowAI_STORAGE__ROOT=./data/uploads

# Web
NEXT_PUBLIC_API_BASE_URL=http://localhost:5080
```

### OpenAI API Key

The OpenAI API key can be configured in two ways:
1. **Environment variable** (`OrchestFlowAI_LLM__OPENAI__API_KEY`): set before starting the API; restart required to pick up changes.
2. **Settings page** (`Settings → OpenAI API Key` in the UI): changes take effect immediately via hot-reload (`OpenAIApiKeyHolder`) — no restart needed.

### ENCRYPTION_MASTER_KEY

Master key for AES-256-CBC encryption of secret vault values. **Must be changed in production.** The default `dev-encryption-key-change-in-production` must never be used in any environment with real secrets. If the key changes after secrets are stored, existing secrets will fail to decrypt.

### Gmail OAuth2

To use the `integrations.gmail.read` node with saved credentials:
1. Register an OAuth2 app in Google Cloud Console and note the client ID and secret.
2. Visit `GET /api/gmail/auth/start?name=my-gmail&clientId=...&clientSecret=...` in a browser.
3. Complete the Google consent flow.
4. The credential is saved under the name `my-gmail` and can be referenced in `GmailReadNode` via `credentialName: "my-gmail"`.

---

## 8. Running Tests

```bash
# Backend
dotnet test OrchestFlowAI.sln

# Frontend
pnpm --filter @OrchestFlowAI/web test
pnpm --filter @OrchestFlowAI/web test:e2e   # Playwright (requires the stack running)
```

---

## 9. Common Tasks

| Task                                    | Command                                                                       |
| --------------------------------------- | ----------------------------------------------------------------------------- |
| Add a database migration                | `dotnet ef migrations add <Name> --project OrchestFlowAI.Infrastructure`         |
| Update DB                               | `dotnet run --project services/OrchestFlowAI.Api -- db update`                    |
| Regenerate OpenAPI client (FE)          | `pnpm --filter @OrchestFlowAI/web codegen`                                        |
| Format / lint                           | `dotnet format` · `pnpm lint` · `pnpm format`                                 |
| Seed demo data                          | `dotnet run --project services/OrchestFlowAI.Api -- seed demo`                    |
| Reset local DB                          | `docker compose down -v postgres && docker compose up -d postgres && dotnet run --project services/OrchestFlowAI.Api -- db update && dotnet run --project services/OrchestFlowAI.Api -- seed demo` |

---

## 10. Troubleshooting

- **Postgres "role does not exist"** → recreate the Docker volume (`docker compose down -v postgres`).
- **`401` from `/api/auth/login`** → verify `OrchestFlowAI_AUTH__JWT_SIGNING_KEY` is at least 32 bytes and matches across services.
- **AI calls fail with `401`** → check `OrchestFlowAI_LLM__OPENAI__API_KEY`; logs will redact the key value.
- **Worker not picking up executions** → confirm Redis is running (or that the Postgres-only queue path is enabled in MVP).
- **Frontend can't reach API** → check `NEXT_PUBLIC_API_BASE_URL` and CORS allowlist in API config.

---

## 11. IDE Setup

- **VS Code:** install the C# Dev Kit, ESLint, Prettier, Tailwind CSS extensions.
- **Rider:** open `OrchestFlowAI.sln` for backend; open `apps/web` separately or via the JS plugin.
- Recommended `.editorconfig` is checked in; honor it.

---

## 12. Tenant Onboarding Flow

OrchestFlowAI provides a guided onboarding experience for creating a new workspace and inviting team members.

### Onboarding Steps

1. **Name your workspace** � Navigate to /onboarding. Enter a workspace name and submit. This calls POST /api/tenants (requires AdminOnly policy).

2. **Invite your team** � Enter a teammate's email and select their role (Admin, Editor, Approver, Viewer). Submit to call POST /api/tenants/{id}/invite. An invite token is returned (in production this would be emailed; for MVP the token is displayed in the UI).

3. **Share the invite link** � The generated invite link is in the format:
   `
   https://<host>/invite/<tenantId>?token=<inviteToken>
   `

4. **Invitee accepts** � The invitee visits /invite/<tenantId>?token=<inviteToken>, sets a password, and submits. This calls POST /api/tenants/{id}/invite/accept which creates their user account. They are then redirected to /login.

### API Endpoints

| Method | Path | Policy | Description |
|--------|------|--------|-------------|
| POST | /api/tenants | AdminOnly | Create a new tenant workspace |
| GET | /api/tenants/{id} | ViewerOrAbove | Get tenant info (own tenant only) |
| POST | /api/tenants/{id}/invite | AdminOnly | Invite a user by email |
| POST | /api/tenants/{id}/invite/accept | Anonymous | Accept invite and create account |

### Invite Lifecycle

- Invites expire after **24 hours**.
- Each invite token is a unique GUID string (32 hex characters).
- Once accepted, the invite cannot be reused.
- The new user's role is set from the invite's 
ole field.

> **Tip:** OPENAI_API_KEY can also be configured at runtime via the Settings page in the UI (Settings ? OpenAI API Key). Changes take effect immediately without restart.
