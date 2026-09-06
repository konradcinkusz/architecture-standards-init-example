# ADR-0005: The API ships PostgreSQL migrations only

- **Status:** Accepted
- **Date:** 2026-09-06
- **Deciders:** repository owner

## Context

P4 makes the provider a configuration switch — `PostgreSQL | SqlServer`, falling
back to InMemory with no connection string — and requires a real provider's schema
to be applied by `MigrateAsync` from **provider-specific** migrations.

Provider-specific is the constraint that bites. EF Core keeps one model snapshot
per `DbContext` per assembly, so genuinely supporting two providers means two
migrations assemblies (or two contexts), which is two more projects.

This system deploys on PostgreSQL. Nothing targets SQL Server.

## Decision

The kernel's `AddDatabaseContext` keeps both providers, because P2's table defines
the kernel's shape and a future service may well target SQL Server.

`ArchitectureStandardsInitExample.Api` ships **PostgreSQL migrations only**, in
`Migrations/`. Selecting `DATABASE_PROVIDER=SqlServer` throws at startup from
`MigrationHostedService`, with a message naming this ADR, rather than failing deep
inside EF with a syntax error from Npgsql-shaped SQL.

## Consequences

- One migrations directory, one snapshot, readable diffs.
- The failure for an unsupported provider is explicit, early, and says what to do
  instead. Letting `MigrateAsync` try instead produces a SQL syntax error naming
  neither the provider nor the cause.
- A service that genuinely needs SQL Server gets its own migrations assembly at
  that point. The kernel needs no change, which is the property that makes this
  cheap to reverse.

## Alternatives considered

- **Two migrations assemblies now.** Faithful to "provider-specific migrations" in
  the fullest sense. Rejected as scaffolding for a provider with no consumer —
  §12's non-goal, and two projects that would be generated, never run, and
  eventually wrong.
- **Drop SQL Server from the kernel's switch.** Tempting, and it would make the
  guard unnecessary. Rejected because the kernel is the estate's, not this
  service's: trimming it to suit the one service that exists today is exactly the
  coupling P2 exists to prevent.
- **`EnsureCreated` on PostgreSQL.** Forbidden by P4, and stated there as a
  correction to the standards' own sources: it records no migration, so the schema
  freezes at first-boot state while migrations accumulate in code, and the first
  request touching a newer column fails. The estate has a live system in exactly
  this condition. `EnsureCreated` appears once in this repository, on the InMemory
  path, where there is nothing to migrate.
