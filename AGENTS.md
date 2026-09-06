# Working in this repository

For an agent — or a person who would rather read one page than five. It points at
the standards rather than restating them, because a second copy of a rule is a
copy that will disagree with the first.

## Read the standards; do not re-derive them

This repository follows the estate's constitution, P1–P15:
[`00-REFERENCE-ARCHITECTURE.md`](https://github.com/konradcinkusz/architecture-standards/blob/main/docs/architecture/00-REFERENCE-ARCHITECTURE.md).
[`.claude/settings.json`](.claude/settings.json) declares that adoption and enables
the `architecture-core` plugin, so the constitution and its guides are readable in
a session here without anyone remembering to attach anything.

**Load the guide for a layer before you change that layer**, and say which one you
loaded. Re-deriving a rule that is already written down is how two files end up
disagreeing about the same thing.

| Touching | Load |
|---|---|
| The kernel, a service, wiring | the constitution, then `SERVICE-API-PATTERNS` |
| The frontend or the BFF | `FRONTEND-BFF` |
| `fly.toml`, a Dockerfile, a deploy workflow | `FLY-IO-DEPLOYMENT` |
| Tests | `TESTING-STRATEGY`, and `E2E-ACCEPTANCE-TESTING` for the Playwright suite |
| Anything about accounts or tokens | `IDENTITY-AND-ACCOUNTS` |
| Repository hygiene, scripts, onboarding | `REPO-BASELINE` |

## Read these before forming an opinion about this repository

- [`docs/architecture/00-ARCHITECTURE.md`](docs/architecture/00-ARCHITECTURE.md) —
  where this repository stands against the constitution, and its **deviation
  register**. Read the register before reporting anything: a deviation already
  recorded with a date and a reason is a decision, not a finding.
- [`docs/adr/`](docs/adr/) — the decisions already taken. A change that contradicts
  an accepted ADR is a request to revisit that ADR, and should say so in those
  words rather than arriving as a silent reversal.
- [`docs/ux/UI-UX.md`](docs/ux/UI-UX.md) — the ranked backlog. Start here rather
  than inventing a direction.

## The things most likely to be got wrong here

Each of these is enforced mechanically, so getting it wrong shows up as a red
build rather than as a review comment — but knowing why saves the round trip.

1. **The kernel is a kernel.** `ServiceDefaults` holds cross-cutting plumbing and
   nothing else. No entity, DTO, enum, seed dataset, pricing constant or
   user-facing string. `KernelBoundaryTests` and the `kernel-size` CI step both
   fail if that changes. Business data belongs to the service that owns it, or to
   `Contracts` if it crosses a boundary.
2. **`EnsureCreated` is for the InMemory path only.** On a real provider it
   records no migration, so the schema freezes at first-boot state while
   migrations accumulate in code. Schema changes are migrations, applied by
   `MigrationHostedService` after the listener is up.
3. **Every new hosted service awaits `MigrationCompletionSignal`.** Skipping it
   races the schema and dies on a missing table once, at 3 a.m., unreproducibly.
4. **A new optional integration degrades, and says so.** Register it conditionally,
   add it to `IntegrationStatus` with a line naming what is lost, and the health
   endpoint and startup banner both pick it up from that one source. If a fresh
   clone stops starting without a credential, this was got wrong.
5. **Every list endpoint clamps `page` and `limit`.** An unclamped limit is a
   one-line outage, not a style nit.
6. **No secret in source, config or comment.** Values come from `user-secrets`
   locally and Fly secrets when deployed. The pre-commit hook and the CI job both
   scan; the hook catches it before it becomes history.
7. **`[build] context` is declared in every `fly.toml`.** Omitting it makes the
   build depend on where `flyctl` was invoked — it works locally and breaks in CI.
8. **A test may not pass without checking anything.** No `if (count === 0) return`,
   no `try { assert } catch {}`, no commented-out body. If a scenario is not
   implemented, skip it with a reason so the CI summary reads honestly.

## Conventions worth knowing before you fight them

- **Formatting is settled by [`.editorconfig`](.editorconfig).** Do not restate it,
  and do not silence an analyzer with a `#pragma` — if a rule should be off, turn
  it off there, scoped, with a reason.
- **Warnings are errors.** A warning nobody has to act on is a warning nobody reads.
- **`dotnet test`, not `dotnet test --nologo`.** In the .NET 10 SDK's MTP mode that
  flag reaches the test application, which rejects it with exit code 5 and the
  misleading summary "Zero tests ran".
- **pnpm, everywhere.** `web/` is a workspace; `tests/e2e` and `docs/diagrams` are
  standalone pnpm packages. A mixed npm/pnpm repository is an anti-pattern with no
  upside.
- **Documentation that a change makes untrue is part of that change**, not a
  follow-up. A stale README is a review finding on the day it is written (P14).

## Before opening a pull request

The [PR template](.github/pull_request_template.md) has the compliance list. The
short version: build clean, tests green and actually asserting, the scanner clean,
the README still true of the tree, and anything you did differently recorded — in
an ADR if it is a decision, in the deviation register if it is a departure from the
constitution. **An acknowledged deviation is a decision; an unacknowledged one is
drift.**

## What not to do

- Do not add a second service, an event bus, a custom DI container, a user store,
  or a sample domain model. Each is on the non-goals list with a reason
  ([`docs/architecture/00-ARCHITECTURE.md` §4](docs/architecture/00-ARCHITECTURE.md)).
- Do not run the deploy from a laptop. App and volume creation live in
  [`.github/workflows/flyio.yml`](.github/workflows/flyio.yml) deliberately, where
  they are reviewable.
- Do not report a gate as passed that you could not run. Say it was not run.
