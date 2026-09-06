# ADR-0003: GHCR is the canonical registry; the deploy mirrors into Fly's

- **Status:** Accepted
- **Date:** 2026-09-06
- **Deciders:** repository owner

## Context

The constitution's §2 disagreement table settles the registry question as
**GHCR** — portable and free at this scale — while the Fly guide's pipeline (§10)
is written against `registry.fly.io`. Both are permitted; the choice is a
recorded decision either way.

Two facts checked while building this, rather than assumed:

1. **A GHCR package is private by default.** GitHub's own documentation is
   explicit that a package inherits the linked repository's *access permissions*
   but **not its visibility**, and that a newly published package is private. A
   public repository does not imply a public image.
2. **Fly has no first-class credential for pulling from a private third-party
   registry.** `flyctl deploy --image ghcr.io/…` on a private package fails at the
   pull, and the documented path for an image Fly can always reach is
   `registry.fly.io` plus `flyctl auth docker`.

Together those mean "GHCR, deploy straight from it" only works if every package is
made public through a manual, per-package setting that lives outside git — which
is exactly the kind of state the standards keep out of shell history.

## Decision

Images are built **once** and pushed to
`ghcr.io/konradcinkusz/architecture-standards-init-example-<service>`, which is the
canonical, portable artifact.

The deploy job then copies the manifest into `registry.fly.io/<app>:<tag>` with
`docker buildx imagetools create` and deploys from there.

## Consequences

- **Build-once-deploy-many holds exactly.** `imagetools create` is a
  registry-to-registry manifest copy, not a second build: the *same digest*
  reaches both registries. The deploy step prints both digests so a divergence
  would be visible rather than assumed.
- No manual visibility step, and no state outside git. A cold estate comes up from
  a single tag.
- The images stay portable: GHCR holds the artifact any other platform would pull,
  so leaving Fly does not mean rebuilding history.
- The cost is one extra step per deployed service and the pull-and-push traffic
  behind it. At two services this is seconds.
- The mirror is the thing to delete if the packages are ever made public — at
  which point `flyctl deploy --image ghcr.io/…` works directly.

## Alternatives considered

- **Push only to `registry.fly.io`.** What the Fly guide's pipeline does, and one
  step shorter. Rejected because it makes Fly the only place the artifact exists,
  which is the portability the constitution chose GHCR to keep.
- **Make each GHCR package public and deploy straight from it.** Removes the
  mirror. Rejected for now because package visibility is a manual setting outside
  the repository, and a new service silently arrives private — so the first deploy
  of every future service would fail at the pull, with a message about
  authentication rather than about visibility.
- **Build twice, once per registry.** Explicitly rejected by P12: the reference
  SaaS does this and documents the cost.
