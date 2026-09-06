<a name="readme-top"></a>

# architecture-standards-init-example

[![Ask Me Anything](https://flat.badgen.net/static/Ask%20me/anything?icon=github&color=black&scale=1.01)](https://github.com/konradcinkusz "Ask me anything")
[![GitHub license](https://flat.badgen.net/github/license/konradcinkusz/architecture-standards-init-example?icon=github&color=black&scale=1.01)](https://github.com/konradcinkusz/architecture-standards-init-example/blob/main/LICENSE "GitHub license")
[![Maintained](https://flat.badgen.net/static/Maintained/yes?icon=github&color=black&scale=1.01)](https://github.com/konradcinkusz/architecture-standards-init-example/commits/main "Maintained")
[![GitHub branches](https://flat.badgen.net/github/branches/konradcinkusz/architecture-standards-init-example?icon=github&color=black&scale=1.01)](https://github.com/konradcinkusz/architecture-standards-init-example/branches "GitHub branches")
[![GitHub commits](https://flat.badgen.net/github/commits/konradcinkusz/architecture-standards-init-example?icon=github&color=black&scale=1.01)](https://github.com/konradcinkusz/architecture-standards-init-example/commits "GitHub commits")
[![GitHub issues](https://flat.badgen.net/github/issues/konradcinkusz/architecture-standards-init-example?icon=github&color=black&scale=1.01)](https://github.com/konradcinkusz/architecture-standards-init-example/issues "GitHub issues")
[![GitHub pull requests](https://flat.badgen.net/github/prs/konradcinkusz/architecture-standards-init-example?icon=github&color=black&scale=1.01)](https://github.com/konradcinkusz/architecture-standards-init-example/pulls "GitHub pull requests")
[![CI](https://github.com/konradcinkusz/architecture-standards-init-example/actions/workflows/ci.yml/badge.svg)](https://github.com/konradcinkusz/architecture-standards-init-example/actions/workflows/ci.yml "CI")
[![Fly.io](https://img.shields.io/badge/Fly.io-24175B?style=for-the-badge&logo=flydotio&logoColor=white)](https://architecture-standards-init-example-web-dev.fly.dev "Live on Fly.io")

A worked run of the estate's greenfield initializer: what
[`architecture-standards`](https://github.com/konradcinkusz/architecture-standards)
produces when it is pointed at an empty repository. One Aspire composition root,
one shared kernel, one service that owns its database, one Next.js surface in
front of it, three Fly.io apps, and seven workflows — all of it compliant with the
constitution on the first commit rather than on a later cleanup pass.

**It ships no domain model, deliberately.** It is here to be read as an example of
the architecture, not used as a product.

## Run it

One command, and it needs no cloud credential of any kind:

```bash
./scripts/setup.sh && dotnet run --project src/ArchitectureStandardsInitExample.AppHost
```

On Windows, `./scripts/setup.ps1` does the same job — `openssl` is not a command
there, so the PowerShell half generates the one local secret with the .NET runtime
instead of an external tool.

Setup checks prerequisites, restores both dependency graphs, **generates** the one
mandatory secret into `dotnet user-secrets` (a human asked to invent one produces
`changeme`), and offers each optional integration as a labelled step so skipping
is informed. `./scripts/setup.sh --check` reports what is missing and changes
nothing.

Without Docker running, or without the AppHost, the API still starts on its own
and falls back to an in-memory database:

```bash
dotnet run --project src/ArchitectureStandardsInitExample.Api
```

That is not a convenience — it is [P8](https://github.com/konradcinkusz/architecture-standards/blob/main/docs/architecture/00-REFERENCE-ARCHITECTURE.md#p8)
made checkable, and the E2E suite in CI boots exactly this way on every pull
request. `GET /health` names every optional integration and what is lost without
it; the startup banner prints the same list from the same source.

## What is here

| Project | What it owns | Deps |
|---|---|---|
| [`src/…AppHost`](src/ArchitectureStandardsInitExample.AppHost/) | The development composition root. Declares Postgres, the API and the frontend with `WithReference`, `WaitFor` and `WithHttpHealthCheck`. **Not** the production topology | 3 |
| [`src/…ServiceDefaults`](src/ArchitectureStandardsInitExample.ServiceDefaults/) | The shared kernel: telemetry, health, discovery, resilience, JWT, CORS, OpenAPI, database provider. 477 lines, and the ceiling is enforced by a test and a CI step rather than by prose | 12 |
| [`src/…Contracts`](src/ArchitectureStandardsInitExample.Contracts/) | The DTOs that cross a boundary. References nothing | 0 |
| [`src/…Api`](src/ArchitectureStandardsInitExample.Api/) | The one service. Owns `apidb`; exposes `/health`, `/alive` and `/api/boots` | 1 |
| [`web/app`](web/app/) | The product surface. The browser's only origin | 3 + 7 dev |
| [`tests/…Api.Tests`](tests/ArchitectureStandardsInitExample.Api.Tests/) | 10 tests: the zero-credential health report, list clamping, the error shape, and three kernel guards | 2 |
| [`tests/e2e`](tests/e2e/) | 4 Playwright tests through the whole stack, run on every pull request | 1 |

A dependency count that goes up in a diff is a question somebody can ask.

## How it works, in four sentences

The **browser talks only to the frontend's own origin**. The frontend's server
side reads its configuration per request from `/api/config` — never from
`NEXT_PUBLIC_*`, which bakes at build time and costs one image per environment —
and reaches the API through a catch-all proxy that walks a candidate ladder, so
one code path works on a laptop, under Aspire, and on Fly. The **API owns its
database** and moves its schema with `MigrateAsync` from a hosted service that
starts *after* Kestrel, so probes answer while migrations are in flight. The
**shared kernel is a kernel**: an architecture test asserts it holds no entity
type and exports nothing you can inherit from, and CI fails it past 800 lines.

## Deploying

Push a tag. That is the whole procedure:

```bash
git tag v0.1.0 && git push origin v0.1.0
```

Change detection always selects a service whose Fly app does not exist, so a
single tag provisions the entire estate from cold — every app, every volume, no
`fly launch`, nothing configured by hand. The one-time human setup is three lines
and is written down in [`flyio/SECRETS.md`](flyio/SECRETS.md).

| | |
|---|---|
| Frontend | <https://architecture-standards-init-example-web-dev.fly.dev> |
| API health | <https://architecture-standards-init-example-api-dev.fly.dev/health> |

## Documentation

| Question | Where |
|---|---|
| Where does this stand against the constitution? | [`docs/architecture/00-ARCHITECTURE.md`](docs/architecture/00-ARCHITECTURE.md) |
| Why is it like this? | [`docs/adr/`](docs/adr/) |
| What should be built next? | [`docs/ux/UI-UX.md`](docs/ux/UI-UX.md) |
| What does this variable do, and what breaks without it? | [`secrets.env.example`](secrets.env.example) |
| What does this error mean? | [`scripts/README.md`](scripts/README.md) — the table is keyed on literal error text |
| Where does a secret live? | [`flyio/SECRETS.md`](flyio/SECRETS.md) |
| What runs where, and what does it cost? | [`flyio/INFRASTRUCTURE-ANALYSIS.md`](flyio/INFRASTRUCTURE-ANALYSIS.md) |
| How should an agent work in here? | [`AGENTS.md`](AGENTS.md) |

## What it deliberately does not have

No second service, no event bus, no service mesh, no custom DI container, no
abstraction over the platform, no user store or token minting anywhere, and no
sample domain model. Each is a design decision with a reason when the time comes,
not a scaffolding default. The reasoning is in
[`docs/architecture/00-ARCHITECTURE.md` §4](docs/architecture/00-ARCHITECTURE.md).

## License

MIT — see [LICENSE](LICENSE).

<p align="right">(<a href="#readme-top">back to top</a>)</p>
