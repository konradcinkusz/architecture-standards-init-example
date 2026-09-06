# Scripts

Every script here runs alone — none of them assumes another has run first, and
each one names what it is missing rather than failing obliquely (REPO-BASELINE §4).

| Script | What it does |
|---|---|
| [`setup.sh`](setup.sh) / [`setup.ps1`](setup.ps1) | One-command onboarding: prerequisites, dependencies, the generated local secret, then each optional integration as a labelled step. `--check` / `-Check` reports what is missing and changes nothing. |
| [`scan-secrets.sh`](scan-secrets.sh) | Local mirror of the `secret-scan` CI job, so the job can be debugged without pushing. `--staged` scans what the hook scans. |
| [`hooks/pre-commit`](hooks/pre-commit) | Installed into `.git/hooks/` by setup. Blocks a commit whose staged changes look like a credential (P5). |

There is no numbered runbook yet, because there is nothing to run in order: the
deploy lives entirely in [`.github/workflows/flyio.yml`](../.github/workflows/flyio.yml),
where it is reviewable, rather than in someone's shell history (FLY-IO §11). When a
second operational script arrives, it joins as `1-<verb>-<noun>` and this table
grows a row — the numeric prefix is the documentation of order, the descriptive
name is what you grep for.

## Every variable, by tier

The authoritative list with its degrade lines is
[`secrets.env.example`](../secrets.env.example); this is the index.

| Variable | Tier | Read by | Degrades to |
|---|---|---|---|
| `DATABASE_PROVIDER` | mode | API | InMemory — a database that is empty after every restart |
| `ConnectionStrings__apidb` | secret | API | InMemory, as above |
| `POSTGRES_PASSWORD` | secret | deploy workflow, postgres app | nothing — the deploy fails, correctly |
| `FLY_API_TOKEN` | ci | deploy workflow | nothing — the deploy fails at the first `flyctl` call |
| `FLY_ORG` | ci | deploy workflow | `personal` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | tuning | API (kernel) | telemetry collected but not exported |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | tuning | API (kernel) | no Azure Monitor export; OTLP unaffected |
| `API_BASE_URL` | tuning | frontend, server side only | the proxy's candidate ladder finds the API anyway in development |
| `ASPNETCORE_ENVIRONMENT` | mode | API | `Development` locally, `Production` in the containers |

**One source of truth per variable** (REPO-BASELINE §8). For local development
that is the AppHost: it sets `ConnectionStrings__apidb` on the API through
`WithReference`, and nothing else may. For deployment it is the app's own
`fly.toml` `[env]` for anything public, and a Fly secret for anything not.

## The one secret's journey, once

```
scripts/setup.sh  ->  dotnet user-secrets   Parameters:postgres-password
                  ->  AppHost               builder.AddParameter("postgres-password", secret: true)
                  ->  postgres container    POSTGRES_PASSWORD
                  ->  API                   ConnectionStrings__apidb   (via WithReference)
```

Deployed, the same key arrives from a different direction and nothing in the
application changes:

```
GitHub Environment `dev`  ->  POSTGRES_PASSWORD
                          ->  flyctl secrets set --stage   ConnectionStrings__apidb=...
                          ->  API                          the same configuration key
```

## Troubleshooting

Keyed on the literal text you will see, because that is what gets pasted into a
search box (REPO-BASELINE §3).

| The error says | What is wrong | Fix |
|---|---|---|
| `The value for parameter 'postgres-password' is missing` | The AppHost has no local secret to hand the database. | `./scripts/setup.sh` — step 3 generates one. Do not invent a value. |
| `No project was found. ... could not find a project or solution file` | You are not in the repository root. | `cd` to the root; every script resolves paths from there itself. |
| `Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:5432` | Something asked for PostgreSQL without one running. | Run through the AppHost (`dotnet run --project src/ArchitectureStandardsInitExample.AppHost`), which starts the container — or unset `DATABASE_PROVIDER` to use InMemory. |
| `docker: Cannot connect to the Docker daemon` | Docker Desktop is not running, and the AppHost wants a Postgres container. | Start Docker Desktop. Or run the API alone (`dotnet run --project src/ArchitectureStandardsInitExample.Api`), which falls back to InMemory. |
| `ERR_PNPM_OUTDATED_LOCKFILE` | `package.json` and `pnpm-lock.yaml` disagree. | `cd web && pnpm install` (no `--frozen-lockfile`), and commit the updated lockfile. |
| `error TS2307: Cannot find module '@/lib/...'` | The frontend's dependencies were never installed. | `cd web && pnpm install`. |
| `warning ... : error CS____` on a build that used to pass | Warnings are errors here (`Directory.Build.props`). | Fix it. Suppressing belongs in `.editorconfig` with a reason, not in a `#pragma`. |
| `pre-commit: gitleaks not installed` | The hook is installed but the scanner is not. | `winget install gitleaks` / `brew install gitleaks`. `--no-verify` delays the check; it does not skip it — CI runs the same scan. |
| `The term 'openssl' is not recognized` | A Unix-only instruction on Windows. | Use `scripts/setup.ps1`; it generates the secret with the .NET runtime and needs no external tool. |
