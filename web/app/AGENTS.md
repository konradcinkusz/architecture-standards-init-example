<!-- BEGIN:nextjs-agent-rules -->

# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` (resolved from this file's directory; in monorepos the `next` package may not be visible from the repo root) before writing any code. Heed deprecation notices.

This block is written and re-added by `next dev` — verify at `node_modules/next/dist/server/lib/generate-agent-files.js`. Removing it from a diff only re-creates the uncommitted change; committing it with your work keeps the tree clean.

<!-- END:nextjs-agent-rules -->

<!-- Everything below is outside the generated block above and survives `next dev`
     rewriting it. -->

## The rules for this repository

The block above is written by Next.js and is about Next.js. The rules for working
in *this* repository — the constitution it follows, what the shared kernel may
not hold, why there is no auth here, and what to read before changing a layer —
are in [`../../AGENTS.md`](../../AGENTS.md).

The three that bite most often in this directory:

- **No `NEXT_PUBLIC_*` for anything environment-specific.** Those bake at build
  time and cost one image per environment. Addresses come from `/api/config`,
  read per request (FRONTEND-BFF §2).
- **The browser talks only to this origin.** Client code never learns a backend
  address and never holds a token; the server side proxies through
  `app/api/proxy/[...path]` (FRONTEND-BFF §1).
- **Shared code goes in a workspace package, not a folder here.** The moment a
  second app or a second screen needs a component, it moves to
  `@architecture-standards-init-example/web-kit` — the workspace exists to make
  that a move rather than a copy (FRONTEND-BFF §7).
