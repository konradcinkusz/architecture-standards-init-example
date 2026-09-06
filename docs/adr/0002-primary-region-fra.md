# ADR-0002: `fra` is the primary region

- **Status:** Accepted
- **Date:** 2026-09-06
- **Deciders:** repository owner

## Context

Every Fly app needs a `primary_region`, and the database needs a volume. A volume
is a **local disk pinned to one machine in one region** — not network storage, not
replicated. Once it holds data, moving region means a dump, a new volume and a
restore, with downtime.

So this is asked once, at t=0, rather than defaulted. The guides show both `waw`
and `fra` in examples; neither is a default.

## Decision

`primary_region = "fra"` (Frankfurt) for all three apps.

## Consequences

- Every machine is created in Frankfurt unless told otherwise, and the volume
  lives there permanently.
- Latency is good across continental Europe and acceptable from the UK; it is
  poor from North America and Asia. If a meaningful share of traffic arrives from
  outside Europe, the answer is a second region for the *stateless* apps — the
  frontend and the API scale horizontally — while the database stays put. That is
  a configuration change, not a migration.
- Changing this later is a data migration with downtime. It is not a config edit,
  and nothing in the pipeline makes it one.

## Alternatives considered

- **`waw` (Warsaw).** Lower latency for Polish users specifically, and the region
  used in the estate's other worked example. `fra` was preferred for broader
  European coverage at a latency difference measured in single-digit
  milliseconds for the nearest users.
- **`iad` (Ashburn).** Correct if the users were primarily North American. They
  are not.
- **Multi-region from the start.** Buys nothing at this size and costs a machine
  per region continuously, plus the question of what to do about a single-region
  database — which is the hard part and is not solved by adding regions to the
  stateless apps. Deferred until there is traffic to justify it.
