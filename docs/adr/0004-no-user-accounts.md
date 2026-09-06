# ADR-0004: This system has no user accounts

- **Status:** Accepted
- **Date:** 2026-09-06
- **Deciders:** repository owner

## Context

The initializer asks this once, explicitly, because the answer changes the shape
of four layers at the same time: whether a fourth Fly app exists, whether the API
validates tokens, whether the frontend has session routes and verifying
middleware, and whether there is a login screen at all.

Answering it silently is the failure this ADR exists to prevent, in both
directions. Guessing "yes" produces a login page for a system with nobody to log
in — scaffolding nobody asked for, which the first ticket deletes. Guessing "no"
and being wrong means retrofitting identity through every layer at once.

## Decision

This system has **no user accounts**. Everything it exposes is public, and every
auth step is skipped:

- No `...-authservice-dev` Fly app.
- The API does not call `AddJwtAuthentication`; its one endpoint group is public.
- The frontend has no `middleware.ts`, no cookie-session routes, and no login
  screen. The BFF proxy injects no bearer token, because there is none to inject.
- This repository holds no key material of any kind.

**What is deliberately kept**: `AddJwtAuthentication` and `AddCorsPolicy` remain
in the shared kernel, unused. P2's table defines the kernel's shape, not today's
consumer list — a service opts in line by line, and the line has to exist to be
opted into. `Program.cs` names both, commented out with the reason, so their
absence reads as a decision rather than an omission.

## Consequences

- The whole of P5 reduces to one sentence for now: there is no signing key here,
  so there is nothing to leak and nothing to rotate.
- The deploy has no non-empty-JWKS assertion, because there is no key set to
  publish. Its place is marked in [`flyio/SECRETS.md`](../../flyio/SECRETS.md) so
  the addition is not re-derived later. That check exists because a missing key
  selects the symmetric path and publishes a syntactically valid, **empty** JWKS:
  nothing reports unhealthy, the deploy goes green, and every consumer rejects
  every token.
- The endpoint triad (public / authenticated / admin) collapses to one group. When
  accounts arrive, the other two are declared in the same file, where a missing
  `RequireAuthorization` is greppable rather than invisible in an attribute.
- Adding accounts later is real work, but it is *additive* and its shape is already
  fixed: adopt [`konradcinkusz/authservice`](https://github.com/konradcinkusz/authservice)
  as an external service, pinned to one machine because every validator fetches its
  JWKS in-request and re-fetches when the cache expires. No user store and no token
  minting is ever written into this repository (P5).

## Alternatives considered

- **Ship identity anyway, on the theory that most systems need it.** Rejected: the
  standards are explicit that a login page for a system with no accounts is
  scaffolding, and the cost is not only code — it is a fourth always-on Fly machine
  and a signing key to look after.
- **A single shared API key instead of accounts.** The shape the estate's
  observability product uses, and a real option for a machine-to-machine surface.
  Rejected because it is not simpler than nothing, and it invites being mistaken
  for authentication later.
