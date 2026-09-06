# ADR-0006: The AppHost uses an evaluation-only Aspire API

- **Status:** Accepted
- **Date:** 2026-09-06
- **Deciders:** repository owner

## Context

`AddNextJsApp` in `Aspire.Hosting.JavaScript` 13.5.3 is marked evaluation-only and
raises `ASPIREJAVASCRIPT001`, which this solution's warnings-as-errors setting
turns into a build failure. Suppressing a diagnostic that says "subject to change
or removal in future updates" is a decision, not a formatting preference, so it is
recorded here rather than left as a bare `NoWarn` for somebody to find.

## Decision

`ASPIREJAVASCRIPT001` is suppressed in
`ArchitectureStandardsInitExample.AppHost.csproj`, and nowhere else, with the
reasoning in a comment at the point of suppression.

The same file suppresses `ASPIRE010` (the Aspire CLI bundle opt-in) on a separate
argument: opting in would add a build-time download to every CI run to enable
features this repository does not use.

## Consequences

- If the API changes shape, the AppHost stops compiling and the fix is one edit to
  one file. That is the whole exposure.
- **Nothing deployed goes through this API.** The AppHost is development-only
  (P1); the deployed frontend is a container described by
  [`flyio/web.fly.toml`](../../flyio/web.fly.toml) and built from its own
  Dockerfile. An evaluation-only API here cannot reach production, because this
  project does not run there.
- The suppression is scoped to one project, so it silences nothing for any future
  consumer of the kernel or the services.

## Alternatives considered

- **`AddNodeApp` or `AddJavaScriptApp`,** neither flagged. Rejected: both would
  need the Next.js dev-server details spelled out by hand, which is what the
  flagged helper exists to get right — and hand-rolled details are more likely to
  drift than an API whose shape may change.
- **Leave the frontend out of the AppHost** and run it in a second terminal.
  Rejected: P1's promise is that one command brings the system up, and a
  composition root missing a component is a composition root that lies.
- **Turn off warnings-as-errors.** Rejected for the obvious reason: one
  inconvenient diagnostic is not a reason to stop reading all of them.
