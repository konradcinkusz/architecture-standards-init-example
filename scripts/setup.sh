#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# One-command onboarding (REPO-BASELINE §3).
#
#   ./scripts/setup.sh            run every step
#   ./scripts/setup.sh --check    report what is missing, change nothing
#
# The PowerShell twin is scripts/setup.ps1. Both exist because a setup
# instruction that only works on one platform fails at step one of onboarding
# for half the team (IDENTITY-AND-ACCOUNTS §10).
# ---------------------------------------------------------------------------
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

APPHOST="src/ArchitectureStandardsInitExample.AppHost"
CHECK_ONLY=false
[[ "${1:-}" == "--check" ]] && CHECK_ONLY=true

missing=0
note()  { printf '  %s\n' "$*"; }
ok()    { printf '  [ ok ] %s\n' "$*"; }
warn()  { printf '  [ -- ] %s\n' "$*"; }
fail()  { printf '  [FAIL] %s\n' "$*"; missing=$((missing + 1)); }
step()  { printf '\n%s\n' "$*"; }

# --- 1. Prerequisites -------------------------------------------------------
step "1. Prerequisites"

if command -v dotnet >/dev/null 2>&1; then
  sdk="$(dotnet --version 2>/dev/null || echo unknown)"
  case "$sdk" in
    10.*) ok ".NET SDK $sdk" ;;
    *)    fail ".NET SDK $sdk found, but this solution targets net10.0.
         Install 10.0: https://dotnet.microsoft.com/download/dotnet/10.0" ;;
  esac
else
  fail ".NET SDK 10 not found — https://dotnet.microsoft.com/download/dotnet/10.0"
fi

if command -v node >/dev/null 2>&1; then
  node_major="$(node --version | sed 's/^v\([0-9]*\).*/\1/')"
  if [[ "$node_major" -ge 20 ]]; then ok "Node $(node --version)"
  else fail "Node $(node --version) found; Next.js needs 20 or newer — https://nodejs.org"; fi
else
  fail "Node 20+ not found — https://nodejs.org"
fi

if command -v pnpm >/dev/null 2>&1; then ok "pnpm $(pnpm --version)"
else fail "pnpm not found — 'corepack enable pnpm', or https://pnpm.io/installation"; fi

# Optional prerequisites: named so that skipping is informed (P8).
if command -v docker >/dev/null 2>&1; then ok "docker $(docker --version | awk '{print $3}' | tr -d ,)"
else warn "docker not found (optional — needed only to build the container images
         locally; 'dotnet run' uses the in-memory database). https://docs.docker.com/get-docker/"; fi

if command -v gitleaks >/dev/null 2>&1; then ok "gitleaks $(gitleaks version 2>/dev/null || echo '')"
else warn "gitleaks not found (optional — needed for the pre-commit secret scan in
         step 4; the CI job runs regardless). 'winget install gitleaks' / 'brew install gitleaks'"; fi

if $CHECK_ONLY; then
  step "Check complete."
  if [[ "$missing" -gt 0 ]]; then
    note "$missing required tool(s) missing — see the FAIL lines above."
    exit 1
  fi
  note "Everything required is installed. Run ./scripts/setup.sh to finish setup."
  exit 0
fi

if [[ "$missing" -gt 0 ]]; then
  step "Stopping: $missing required tool(s) missing."
  note "Install them and run this script again."
  exit 1
fi

# --- 2. Dependencies --------------------------------------------------------
step "2. Restoring dependencies"
dotnet restore ArchitectureStandardsInitExample.slnx
ok ".NET packages restored"
( cd web && pnpm install --frozen-lockfile )
ok "frontend packages installed"

# --- 3. The one mandatory secret, generated ---------------------------------
# Generated, never invented: a human asked to make up a secret produces
# `changeme`, and `changeme` reaches production (IDENTITY-AND-ACCOUNTS §10).
# Hex only — `+`, `/`, `=` and `;` all mean something inside a connection string.
step "3. Local secret store"
dotnet user-secrets init --project "$APPHOST" >/dev/null 2>&1 || true

if dotnet user-secrets list --project "$APPHOST" 2>/dev/null | grep -q '^Parameters:postgres-password'; then
  ok "postgres-password already present in user-secrets — left untouched"
else
  generated="$(openssl rand -hex 24 2>/dev/null \
    || head -c 24 /dev/urandom | od -An -tx1 | tr -d ' \n' \
    || date +%s%N | sha256sum | head -c 48)"
  dotnet user-secrets set "Parameters:postgres-password" "$generated" --project "$APPHOST" >/dev/null
  ok "generated postgres-password into user-secrets (48 hex chars)"
fi

note ""
note "  Where that value goes, once:"
note "    dotnet user-secrets  ->  AppHost AddParameter(\"postgres-password\", secret: true)"
note "      ->  the postgres container's POSTGRES_PASSWORD"
note "      ->  ConnectionStrings__apidb, injected into the API by WithReference"
note "  In deployment the same key is a Fly secret instead. See flyio/SECRETS.md."

# --- 4. Optional: the pre-commit secret scan --------------------------------
step "4. Pre-commit secret scan  (optional — needed to catch a credential before it becomes history)"
if command -v gitleaks >/dev/null 2>&1; then
  cp scripts/hooks/pre-commit .git/hooks/pre-commit
  chmod +x .git/hooks/pre-commit
  ok "hook installed at .git/hooks/pre-commit"
else
  warn "skipped: gitleaks is not installed."
  note "       Without it a credential can reach your local history. The CI job"
  note "       still blocks it from reaching the default branch."
fi

# --- 5. Optional: telemetry export ------------------------------------------
step "5. Telemetry export  (optional — needed to send traces somewhere other than the Aspire dashboard)"
if [[ -n "${OTEL_EXPORTER_OTLP_ENDPOINT:-}" ]]; then
  ok "OTEL_EXPORTER_OTLP_ENDPOINT is set to $OTEL_EXPORTER_OTLP_ENDPOINT"
else
  warn "not set. Telemetry is still collected and shown in the Aspire dashboard;"
  note "       it is simply not exported anywhere else. No feature is lost."
fi

# --- Done -------------------------------------------------------------------
step "Setup complete."
note ""
note "  Run the whole system:   dotnet run --project $APPHOST"
note "  Run the tests:          dotnet test"
note "  Scan for secrets:       ./scripts/scan-secrets.sh"
note ""
note "  If something fails, the troubleshooting table in scripts/README.md is keyed"
note "  on the exact error text — paste the message and search for it."
