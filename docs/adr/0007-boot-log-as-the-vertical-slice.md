# ADR-0007: The vertical slice is a boot log, not an invented domain

- **Status:** Accepted
- **Date:** 2026-09-06
- **Deciders:** repository owner

## Context

The template has to ship *some* end-to-end path, or the mechanism is unexercised:
an entity, a migration, an endpoint group, a clamped list, a test, and a screen
that renders it. But §12's non-goals are explicit that there is **no sample domain
model** — inventing entities for a product nobody has described produces code the
first ticket deletes, and worse, code that gets copied because it is there.

Both constraints apply at once, and the usual template answer (a notes CRUD)
satisfies the first by violating the second.

## Decision

The slice is a **boot log**. The API writes one `BootRecord` per start — version,
environment, resolved provider, and which optional integrations were degraded —
exposes it at `/api/boots`, and the one screen renders it.

## Consequences

- Nothing here is a guess about a product. The data is about the system itself, so
  no first ticket has to delete it, and nobody will mistake it for a domain
  example worth copying.
- It exercises everything the mechanism needs to prove: an owned database, a
  migration applied by a hosted service after the listener is up, a second hosted
  service that **awaits the migration completion signal** rather than racing it, a
  clamped list endpoint, the uniform error shape, and a server-rendered screen that
  reaches the API through the BFF proxy.
- It is useful rather than decorative. A row in the deployed boot log proves the
  schema really migrated, says which tag is actually running, and names the
  integrations that deployment is missing. The deploy's post-deploy assertions are
  built on exactly that and would have nothing to check without it.
- It grows by one row per restart. At this scale that is negligible; if it ever is
  not, the fix is a retention sweep — a hosted service this repository already has
  the shape for, and one that would have to await the same migration signal.

## Alternatives considered

- **A notes / todo / items CRUD entity.** The conventional template slice.
  Rejected as precisely the invented domain §12 names, and the one most likely to
  be extended by accident rather than deleted on purpose.
- **No persisted slice at all, just `/health`.** Rejected: it would leave the
  migration path, the completion signal, the clamped list and the error shape
  unexercised and unverified, and the deploy would have nothing to assert beyond
  "it started" — which is what `--wait-timeout` already covers.
- **A generic `SampleEntity`.** Honest about being a placeholder, and useless: it
  proves the mechanism while telling a reader nothing, and still has to be deleted.
