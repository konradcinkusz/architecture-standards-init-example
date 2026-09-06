# Secrets: what is one, where it lives, how it is set

The split is not a matter of taste (FLY-IO §9):

| | Where it lives | Visible in | Set by |
|---|---|---|---|
| Non-secret configuration | `[env]` in a `fly.toml` | a git diff, `fly config show`, image metadata | the committed file |
| Secrets | the Fly secret store | nothing — names and digests only | `flyctl secrets set`, **from the pipeline** |

The test, when it is not obvious: would you paste it into a pull request?

## The one-time human setup

Three things, once per repository. Everything else is created by the pipeline.

1. **A Fly deploy token**, stored as `FLY_API_TOKEN` in a GitHub **Environment**
   named `dev` — an environment rather than a repository secret, because an
   environment can be reviewed and restricted.

   ```bash
   fly tokens create org
   ```

2. **The database password**, stored as `POSTGRES_PASSWORD` in the same
   environment. Hex or alphanumeric only — `+`, `/`, `=` and `;` all mean
   something inside a connection string, a YAML scalar or a shell argument, and
   the bug surfaces one rotation later in a component nobody was touching
   (IDENTITY-AND-ACCOUNTS §10).

   PowerShell, no external tool needed:

   ```powershell
   [System.Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(24)).ToLowerInvariant()
   ```

   bash:

   ```bash
   openssl rand -hex 24
   ```

3. **Nothing else.** No app creation, no volume creation, no `fly launch`. Those
   are idempotent steps in [`flyio.yml`](../.github/workflows/flyio.yml), where
   they are reviewable, instead of in somebody's shell history (FLY-IO §11).

## What the pipeline derives

The GitHub Environment holds the small set of *root* secrets. Everything else is
assembled by the workflow, so no per-service credential is stored anywhere:

| Fly app | Secret | Assembled from |
|---|---|---|
| `…-postgres` | `POSTGRES_PASSWORD` | the root secret, passed through |
| `…-api-dev` | `ConnectionStrings__apidb` | `POSTGRES_PASSWORD` + the known internal host, user and database name |
| `…-web-dev` | none | the frontend holds no credential; it reaches the API through its own server side |

Secrets are set with `--stage`, which holds the change until the next deploy, so
one release does not restart a service twice.

```bash
flyctl secrets set -a <app> "Key__SubKey=value" --stage
flyctl secrets list -a <app>      # names and digests; values are never readable back
```

## The journey one value takes

Locally, and in deployment, the same configuration key arrives from two different
directions and nothing in the application changes:

```
local     scripts/setup.sh -> dotnet user-secrets  Parameters:postgres-password
                           -> AppHost parameter    (secret: true)
                           -> postgres container   POSTGRES_PASSWORD
                           -> API                  ConnectionStrings__apidb

deployed  GitHub Environment `dev`                 POSTGRES_PASSWORD
                           -> flyctl secrets set --stage
                           -> API                  ConnectionStrings__apidb
```

## What must fail the deploy rather than degrade

Optional dependencies degrade (P8). A database credential is not optional: a
service that boots without one falls back to an in-memory database and will look
perfectly healthy while losing every write on restart. The deploy workflow checks
for `POSTGRES_PASSWORD` explicitly and exits non-zero, which is the correct place
to fail.

## This system holds no signing key

There is no identity service here and no user accounts (ADR-0003), so there is no
key material in this repository at all — no signing key, no JWKS, nothing to
rotate. When the system gains accounts, the identity service is adopted as an
external service and **this repository still holds no signing key**: exactly one
service in the estate holds one, every other validates against its published
JWKS, and a symmetric secret shared between services means verify = mint (P5).

The deploy assertion that goes with that — *the published key set must be
non-empty* — is not in this pipeline because there is no key set to publish. It
is the first thing to add alongside the identity service, because a missing key
selects the symmetric path and publishes a syntactically valid, **empty** JWKS:
nothing reports unhealthy, the deploy goes green, and every consumer rejects
every token.

## If a secret reaches the repository

Rotate first, clean history second. The commit is public the moment it is pushed;
scrubbing without rotating is theatre.

1. Rotate the value at its source (Fly, GitHub, the provider).
2. Update the GitHub Environment.
3. Re-run the deploy.
4. Then, and only then, deal with the history.

The pre-commit hook and the `secret-scan` job exist so this section stays
hypothetical — the hook catches it before it becomes history, the job catches
contributors without hooks.
