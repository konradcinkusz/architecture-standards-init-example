# Infrastructure: topology, sizing and cost

Fly.io's §13 asks four questions. "Could be optimised" is not an answer to any of
them; a decision somebody can take is.

## The topology

Three apps, in `fra`. Region chosen for proximity to the users, and recorded as
[ADR-0002](../docs/adr/0002-primary-region-fra.md) because a region is expensive
to change once volumes exist — a volume is a local disk pinned to one machine in
one region, and moving it is a dump and restore.

```
                    browser
                       │  https
                       ▼
        ┌──────────────────────────────┐
        │  …-web-dev                   │   min_machines_running = 0
        │  Next.js standalone, :8080   │   entered only from a browser
        └──────────────┬───────────────┘
                       │  https, server-side only (public URL, not .internal)
                       ▼
        ┌──────────────────────────────┐
        │  …-api-dev                   │   min_machines_running = 1
        │  ASP.NET Core, :8080         │   the frontend calls it in-request
        └──────────────┬───────────────┘
                       │  6PN, :5432
                       ▼
        ┌──────────────────────────────┐
        │  …-postgres                  │   no public listener, ever
        │  postgres:17-alpine + volume │   apidb
        └──────────────────────────────┘
```

There is no identity service, because this system has no user accounts
([ADR-0004](../docs/adr/0004-no-user-accounts.md)). If it gains them, a fourth app
joins between the frontend and the API — `…-authservice-dev`, pinned to one
machine, because every validator fetches its JWKS in-request and re-fetches when
the cache expires.

## 1. What runs when nothing is happening?

| App | Machines when idle | Memory | Volume |
|---|---|---|---|
| `…-postgres` | 1 — a database that scales to zero is a database that loses its connections | 1 GB | 3 GB |
| `…-api-dev` | 1 — pinned; see question 2 | 512 MB | — |
| `…-web-dev` | 0 — the proxy starts it on the first request | 512 MB | — |

Two machines and one volume, continuously. Stopped machines cost their volume, if
any, and nothing else.

## 2. Which services pin a machine, and which synchronous call forces it?

**`…-api-dev` pins one machine.** The call that forces it: the frontend's
server-side page render does `fetch(${API_BASE_URL}/api/boots?limit=10)` while
producing the HTML — see [`web/app/app/page.tsx`](../web/app/app/page.tsx). That is
an in-request call, so the rule applies mechanically: either the callee keeps a
machine running, or the caller's timeout comfortably exceeds the callee's cold
start. The proxy's timeout is 30 s and a .NET cold start on `shared-cpu-1x` is
well inside that, so the second option would work — and it is exactly the option
that gets written down far more often than it gets configured. The machine is
pinned instead.

Naming the call is the point: when the page stops fetching server-side, this
justification is gone and the pin should go with it.

**`…-web-dev` does not.** Nothing calls it in-request. A cold start there is a
slow first page, not a failed call.

**`…-postgres` is not a scale-to-zero question at all.** It has a volume, so it
cannot be scaled horizontally either: a second machine would get a second, empty
volume rather than a replica. It is deployed `--ha=false` and excluded from the
scale workflow.

## 3. What is the cheaper option, and what does it actually cost?

**Let the API scale to zero as well.** Saves one always-on `shared-cpu-1x`/512 MB
machine. The price is a several-second stall on the first page view after an idle
period, every time, because the page render waits for the API to boot — and a
first-time visitor's first impression is exactly that stall. That is a decision
somebody can take, and the trade is legible: one machine's cost against a cold
first page.

**Shrink the volume.** 3 GB is already near the floor and the boot log grows by a
row per deploy, so this saves almost nothing and cannot be undone — growing a
volume later is possible, shrinking is not.

**Drop the frontend and serve the API's OpenAPI page.** Cheaper by one app, and it
gives up having a product surface at all. Recorded here only so it is visibly
considered rather than silently missing.

## 4. What is off the table?

Say so, so nobody re-proposes them:

- **Giving Postgres a public IP.** It is reachable at
  `architecture-standards-init-example-postgres.internal:5432` over 6PN, and from a
  laptop through `fly proxy 15432:5432 --app architecture-standards-init-example-postgres`.
  A database with a public listener is a database waiting to be scanned.
- **Turning off `force_https`.**
- **Sharing one database between services.** One instance is a cost decision and
  is fine; one *database* with two owners is not. Each service gets a connection
  string to exactly one database and no credentials for the others, so splitting
  into separate instances later is configuration rather than code (P3).
- **Scaling any app with a volume past one machine.** `flyctl scale count 2` on
  `…-postgres` creates a second empty database, not a replica. The scale workflow
  excludes it by name.
- **Running `fly launch`, or creating apps and volumes by hand.** They are
  idempotent steps in the deploy workflow, where they are reviewable. `fly launch`
  writes a config nobody reviewed and creates an app whose settings exist nowhere
  in git.

## The registry, and why the deploy has a mirror step

Images are built once and pushed to **GHCR**
([ADR-0003](../docs/adr/0003-ghcr-as-the-canonical-registry.md)) — the
constitution's answer, portable and free at this scale.

Fly has no first-class credential for pulling from a private third-party registry,
and a GHCR package is private by default. So the deploy job copies the image
manifest from GHCR into `registry.fly.io` and deploys from there. It is a copy,
not a rebuild: `docker buildx imagetools create` moves the manifest, so **the same
digest reaches both registries** and build-once-deploy-many holds exactly.

The alternative — making each GHCR package public and deploying straight from it —
removes the mirror step at the cost of a manual, per-package visibility change
that lives outside git. That trade is recorded in the ADR; if the packages are
ever made public, the mirror step is the thing to delete.
