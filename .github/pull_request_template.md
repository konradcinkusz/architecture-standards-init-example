## What this changes

<!-- One paragraph. What is different after this merges, in terms a reviewer who
     did not write it can check. -->

## Why

<!-- The reasoning, not the steps. A decision that a reader might otherwise think
     was inevitable belongs in docs/adr/ as well. -->

## Acceptance criteria

<!-- One line per criterion, each with the test that proves it. A criterion with
     no proving test is not delivered. -->

- [ ]

## Compliance

The layers this diff touches, checked against the constitution
([00-REFERENCE-ARCHITECTURE](https://github.com/konradcinkusz/architecture-standards/blob/main/docs/architecture/00-REFERENCE-ARCHITECTURE.md) §3).
Tick what applies; strike through what the diff does not touch.

- [ ] The shared kernel still holds only plumbing — no entity, DTO, enum, seed dataset, pricing constant or user-facing string (P2)
- [ ] No service reads another service's database (P3)
- [ ] Schema changes ship as migrations applied by the hosted service, not `EnsureCreated` (P4)
- [ ] No secret in source, config file, or comment; the scanner passes (P5)
- [ ] Every new optional integration degrades, and `/health` plus the startup banner report it (P8)
- [ ] `Program.cs` is still a manifest; wiring is in `ServiceCollectionExtensions` (P9)
- [ ] New list endpoints clamp `page`/`limit`; new outbound clients carry resilience and an explicit timeout
- [ ] Tests exist at the layer that holds the logic, and they assert an outcome (P13)
- [ ] Documentation that this change makes untrue has been updated in the same commit (P14)

## Recorded decisions

<!-- Anything you did that departs from a standard, with its reason. An
     acknowledged deviation is a decision; an unacknowledged one is drift.
     Deviations that outlive this PR go in docs/architecture/00-ARCHITECTURE.md's
     deviation register with a date. -->

None.

## How this was verified

<!-- What you actually ran, and what it said. "Should work" is not a line item. -->
