# P6 — the Node three-stage pattern: deps -> builder -> runner, Next.js
# `standalone` output, non-root user.
#
# Build context is `web/` — the pnpm WORKSPACE root, not this directory, because
# the install needs pnpm-workspace.yaml and the lockfile. flyio/web.fly.toml
# declares `context = "../web"` to match; omitting it is what makes a build work
# locally and break in CI (FLY-IO §4).

# ---- deps -------------------------------------------------------------------
FROM node:22-alpine AS deps
RUN corepack enable
WORKDIR /workspace

# Manifests and the lockfile only, so a source change does not re-resolve the
# dependency graph.
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml .npmrc ./
COPY app/package.json app/
RUN pnpm install --frozen-lockfile

# ---- builder ----------------------------------------------------------------
FROM node:22-alpine AS builder
RUN corepack enable
WORKDIR /workspace

# `.npmrc` sets node-linker=hoisted, so the whole graph lives at the workspace
# root and there is no per-package node_modules to copy.
COPY --from=deps /workspace/node_modules ./node_modules
COPY . .

# No build arguments carrying addresses. Anything environment-specific baked in
# here costs one image per environment and breaks build-once-deploy-many (P12);
# the runtime /api/config route reads the environment per request instead.
ENV NEXT_TELEMETRY_DISABLED=1
RUN pnpm --filter web build

# ---- runner -----------------------------------------------------------------
FROM node:22-alpine AS runner
WORKDIR /app

LABEL org.opencontainers.image.source="https://github.com/konradcinkusz/architecture-standards-init-example"
LABEL org.opencontainers.image.description="The product surface of architecture-standards-init-example."
LABEL org.opencontainers.image.licenses="MIT"

ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=8080 \
    HOSTNAME=0.0.0.0
EXPOSE 8080

RUN addgroup --system --gid 1001 nodejs \
 && adduser  --system --uid 1001 nextjs

# `standalone` emits a self-contained server plus only the traced dependencies.
# Because the tracing root is the workspace, the server lands under app/.
COPY --from=builder --chown=nextjs:nodejs /workspace/app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /workspace/app/.next/static ./app/.next/static
COPY --from=builder --chown=nextjs:nodejs /workspace/app/public ./app/public

USER nextjs

CMD ["node", "app/server.js"]
