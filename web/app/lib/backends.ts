/**
 * The candidate ladder (FRONTEND-BFF §5).
 *
 * Each backend is resolved by trying addresses in a fixed order: an explicit
 * environment variable, then the orchestrator's service-discovery variables,
 * then the internal DNS name, then localhost. This is what makes *one* code path
 * work on a laptop, under Aspire, and on Fly, with no per-environment branch.
 *
 * Everything here runs server-side only. The browser never learns a backend
 * address, because it never talks to one.
 */

export type BackendKey = "api";

/** Path prefix -> backend, so the client has exactly one base URL. */
const ROUTES: ReadonlyArray<{ prefix: string; backend: BackendKey }> = [
  { prefix: "api", backend: "api" },
];

const DEFAULT_BACKEND: BackendKey = "api";

export function backendFor(pathSegments: readonly string[]): BackendKey {
  const first = pathSegments[0];
  return ROUTES.find((route) => route.prefix === first)?.backend ?? DEFAULT_BACKEND;
}

/**
 * Candidates in the order they are tried. Nulls are dropped, so a rung that does
 * not apply in this environment simply is not attempted.
 */
export function candidatesFor(backend: BackendKey): string[] {
  const candidates: Array<string | undefined> = [
    // 1. Explicit configuration always wins. This is the rung a deployment uses.
    process.env[`${backend.toUpperCase()}_BASE_URL`],

    // 2. Aspire's service-discovery variables, injected by WithReference.
    process.env[`services__${backend}__https__0`],
    process.env[`services__${backend}__http__0`],

    // 3. Internal DNS. On Fly this is 6PN; note it does NOT auto-start a stopped
    //    machine, which is why the deployed frontend sets API_BASE_URL to the
    //    public URL instead (FLY-IO §6).
    process.env.FLY_APP_NAME ? `http://${backend}.internal:8080` : undefined,

    // 4. A developer running the service by hand.
    "http://localhost:5180",
  ];

  return candidates
    .filter((candidate): candidate is string => typeof candidate === "string" && candidate.length > 0)
    .map((candidate) => candidate.replace(/\/+$/, ""));
}
