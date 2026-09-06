# End-to-end acceptance suite

One journey, four assertions, and **its CI wiring in the same commit** — a suite
no job runs is documentation that lies (E2E-ACCEPTANCE-TESTING §6). The `e2e` job
in [`ci.yml`](../../.github/workflows/ci.yml) runs this on every pull request.

## What it protects

The whole path, in one test: browser → the page → `/api/proxy` → the API →
`apidb` → a boot record rendered back. If any link breaks, this goes red.

The other three cover the rules that are easy to break invisibly: the runtime
config route reading the environment per request (not baked into the bundle), the
API's list clamp surviving the proxy hop, and the uniform error shape.

## Running it

```bash
cd web && pnpm --filter web build     # the suite starts the standalone server
cd ../tests/e2e && pnpm install && pnpm exec playwright install chromium
pnpm test
```

With `E2E_BASE_URL` set, the suite runs against that address and starts nothing —
which is how it points at a deployed environment:

```bash
E2E_BASE_URL=https://architecture-standards-init-example-web-dev.fly.dev pnpm test
```

## The rules this suite is held to

- **A test may not pass without checking anything.** No `if (count === 0) return`,
  no `try { assert } catch {}`, no commented-out body. A scenario that is not
  implemented gets `test.skip` with a reason, so the CI summary reads honestly.
- **Locators are role- and accessible-name-based**, with `data-testid` as the
  deliberate fallback for elements no accessible name reaches. There is exactly
  one testid in this repository, on the boot-log table, and it is commented at
  the point of use.
- **No fixed sleeps.** Web-first assertions only; they poll until they pass.
- **Budget: this is the smoke tier — under 10 minutes, run on every PR.** If it
  grows past that, it gets pruned rather than renamed (TESTING-STRATEGY §2).
