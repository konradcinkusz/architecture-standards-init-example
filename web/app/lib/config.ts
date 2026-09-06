/**
 * Client-side access to the runtime configuration (FRONTEND-BFF §2).
 *
 * The promise is cached so N components mounting at once produce one request
 * rather than N, and the fallback is SSR-safe so a server render does not throw
 * on a fetch that has not resolved.
 */

export type RuntimeConfig = {
  environment: string;
  version: string;
  /** Where the browser sends API calls: this app's own origin, always. */
  apiBasePath: string;
};

const FALLBACK: RuntimeConfig = {
  environment: "unknown",
  version: "0.0.0",
  apiBasePath: "/api/proxy",
};

let inFlight: Promise<RuntimeConfig> | null = null;

export function getRuntimeConfig(): Promise<RuntimeConfig> {
  if (typeof window === "undefined") {
    return Promise.resolve(FALLBACK);
  }

  inFlight ??= fetch("/api/config", { cache: "no-store" })
    .then((response) => (response.ok ? response.json() : FALLBACK))
    .catch(() => FALLBACK);

  return inFlight;
}
