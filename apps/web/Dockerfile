# Build context must be the repo root (.):
#   docker compose -f docker-compose.full.yml up -d --build
# or:
#   docker build -f apps/web/Dockerfile -t orchest-web .

FROM node:22-alpine AS base
# Install pnpm directly — avoids corepack's lazy network download inside the container
RUN npm install -g pnpm@11
WORKDIR /repo

# ── install deps ──────────────────────────────────────────────────────────────
FROM base AS deps
# Copy all workspace manifests so pnpm can resolve the full dependency graph
COPY pnpm-workspace.yaml pnpm-lock.yaml package.json ./
COPY apps/web/package.json      ./apps/web/
COPY apps/marketing/package.json ./apps/marketing/
COPY ui/web-public/package.json  ./ui/web-public/
# Copy shared package source so the workspace symlink can resolve at install time
COPY ui/web-public/src ./ui/web-public/src
RUN pnpm install --no-frozen-lockfile --ignore-scripts

# ── build ─────────────────────────────────────────────────────────────────────
FROM base AS builder
# Copy installed packages (layer cache hit when lockfile unchanged)
COPY --from=deps /repo/node_modules              ./node_modules
COPY --from=deps /repo/apps/web/node_modules    ./apps/web/node_modules
COPY --from=deps /repo/ui/web-public/node_modules ./ui/web-public/node_modules
# Copy full source (node_modules excluded by .dockerignore)
COPY . .
ENV NEXT_TELEMETRY_DISABLED=1
# Enables output: 'standalone' in next.config.ts for the runner stage
ENV DOCKER_BUILD=1
WORKDIR /repo/apps/web
# Call next directly — bypasses pnpm's runDepsStatusCheck entirely
RUN node_modules/.bin/next build

# ── runner ────────────────────────────────────────────────────────────────────
FROM node:22-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

RUN addgroup --system --gid 1001 nodejs \
 && adduser  --system --uid 1001 nextjs

# standalone/ is rooted at the pnpm workspace root, so server.js lands at apps/web/server.js.
# server.js does process.chdir(__dirname) so all paths resolve from /app/apps/web/.
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/standalone      ./
COPY --from=builder --chown=nextjs:nodejs /repo/apps/web/.next/static          ./apps/web/.next/static
COPY --from=builder                        /repo/apps/web/public               ./apps/web/public

USER nextjs
EXPOSE 3000
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
CMD ["node", "apps/web/server.js"]
