import { NextResponse } from "next/server";

/**
 * The runtime configuration route (FRONTEND-BFF §2).
 *
 * `force-dynamic` is the whole point: this must read the environment on every
 * request, not at build time. `NEXT_PUBLIC_*` would bake these values into the
 * bundle and cost one image per environment, which breaks build-once-deploy-many
 * (P12) — the failure mode is a staging frontend calling the production API.
 */
export const dynamic = "force-dynamic";

export async function GET() {
  return NextResponse.json(
    {
      environment: process.env.APP_ENVIRONMENT ?? process.env.NODE_ENV ?? "development",
      version: process.env.APP_VERSION ?? "0.0.0",

      // Client-safe by construction: it is this app's own origin. No backend
      // address is ever returned here — the browser has no use for one and
      // publishing it invites a direct call that bypasses the BFF.
      apiBasePath: "/api/proxy",
    },
    {
      headers: {
        // Short, with stale-while-revalidate: long enough to absorb a burst of
        // mounts, short enough that a promoted image does not keep serving the
        // previous environment's answers.
        "Cache-Control": "public, max-age=10, stale-while-revalidate=30",
      },
    },
  );
}
