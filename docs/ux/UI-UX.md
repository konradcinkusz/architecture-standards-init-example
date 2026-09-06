# UI and UX

What the product surface is today, and what should be built next. The backlog at
the end is ranked so the first delivery session picks it up instead of re-deriving
it.

## The surface, as it exists

One route, plus three that exist to be called rather than looked at.

| Route | Kind | What it is for |
|---|---|---|
| `/` | server-rendered page | The boot log: one row per recorded start of the API |
| `/healthz` | JSON | Exists only to be checked by the platform. Never rendered |
| `/api/config` | JSON | Runtime configuration, read per request |
| `/api/proxy/[...path]` | pass-through | The only way the browser reaches the estate |

### The one screen

A heading, a sentence of orientation, and a table of the API's recorded starts:
when, which build, which environment, which persistence provider, and which
optional integrations were degraded at that start.

It is deliberately a *diagnostic* screen rather than a product screen, for the
reason in [ADR-0007](../adr/0007-boot-log-as-the-vertical-slice.md): a product
screen here would be a guess about a product nobody has described. This one earns
its place by being the cheapest end-to-end proof the system works — if it shows a
row, the page, the BFF proxy, the API, the schema and the boot recorder all worked.

### The flow, such as it is

```
browser  ──GET /──▶  Next.js server component
                       └── fetch(API_BASE_URL/api/boots?limit=10)   server-side
                              └── API ──▶ apidb
                     ◀── HTML with the table already in it
```

There is no client-side data fetching on this screen and no loading state,
because the data is in the HTML by the time the browser has it. That is a
deliberate default rather than an oversight: server-render first, and add
client-side fetching when something actually needs to change without a navigation.

## What is deliberately absent

Each of these is a decision, so nobody files it as a gap:

- **No login, no account menu, no protected route.** This system has no user
  accounts ([ADR-0004](../adr/0004-no-user-accounts.md)).
- **No navigation.** One route does not need a nav bar; adding one now would be
  furniture for rooms that do not exist.
- **No design system, no component library, no CSS framework.** One screen of
  semantic HTML with about 40 lines of CSS. The moment there is a second screen
  with shared components, those components go in a workspace package
  (`@architecture-standards-init-example/web-kit`), not into a folder inside this
  app — that is the rule the workspace exists to make cheap (FRONTEND-BFF §7).
- **No loading skeletons or empty-state illustrations.** The one empty state is a
  line of italic text, which is the correct amount of design for a table that is
  empty for about two seconds after a cold deploy.

## Accessibility and responsiveness, as they stand

Honest rather than aspirational — this is one page and it has not been through an
accessibility pass:

- Semantic HTML: one `h1`, real `table`/`thead`/`th` markup, so a screen reader
  announces the table as a table and the E2E suite can locate by role.
- The colour scheme follows `prefers-color-scheme`; nothing is conveyed by colour
  alone — the degraded integrations are named in text, not shown as a red dot.
- The table scrolls inside its own container, so the page body never scrolls
  horizontally on a narrow viewport.
- **Not done:** keyboard-only walkthrough, contrast measurement against WCAG AA,
  200% zoom, screen-reader pass. These are in the backlog, not silently assumed.

## Backlog, ranked

Ranked by what unblocks the most, not by what is most fun. Each item names the
guide it should be built against, so the next session does not re-derive the rules.

| # | Item | Why it is here | Read first |
|---|---|---|---|
| 1 | **Decide what this product is for.** Everything below is furniture until there is a domain. | The template deliberately shipped no domain model. The first real ticket replaces the boot log as the reason the screen exists — it does not delete it. | §12 non-goals |
| 2 | **An accessibility pass on the one screen** — keyboard, contrast, zoom, screen reader | Cheapest while there is one screen. Retrofitting across ten costs ten times as much, and the estate's worked example never did it. | TESTING-STRATEGY §7 |
| 3 | **A second screen, and with it the `web-kit` package** | The workspace exists for this. The moment two apps or two screens share a component, it goes in the package rather than being copied — the guide's anti-example is four apps with four hand-copied copies of security-relevant code. | FRONTEND-BFF §7 |
| 4 | **Error and empty states as a first-class category** | Today a failed API fetch renders one sentence. That is adequate for a diagnostic page and inadequate for anything a user depends on. | TESTING-STRATEGY §7 |
| 5 | **User accounts, if the answer to #1 needs them** | Adopt `konradcinkusz/authservice` as an external service; add the fourth Fly app pinned to one machine; add cookie-session routes and verifying middleware; add the non-empty-JWKS post-deploy assertion. No user store or token minting is ever written here. | IDENTITY-AND-ACCOUNTS, FRONTEND-BFF §3–§4 |
| 6 | **Pagination controls on the boot log** | The API clamps and pages already; the screen shows the first ten and no way to reach the eleventh. Small, and it exercises the client half of the contract. | SERVICE-API-PATTERNS §4 |
| 7 | **A retention sweep for boot records** | One row per restart is negligible now and unbounded eventually. It is also the second hosted service, which means it must await the migration completion signal — the rule that is easy to miss and fails once, at 3 a.m. | SERVICE-API-PATTERNS §7 |
| 8 | **PR preview environments** | Would let the E2E suite run against a real deployment instead of a locally-booted stack, which is what the guide actually prefers. Worth it at the second service, not the first. | PR-PREVIEW-ENVIRONMENTS |

## The rule this document is held to

A stale document that describes a system that no longer exists is worse than none
(P14). Every claim above was true of the tree at the commit that introduced it. If
a change makes a row here wrong, the row is part of that change, not a follow-up.
