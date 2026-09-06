import { NextRequest, NextResponse } from "next/server";
import { backendFor, candidatesFor } from "@/lib/backends";

/**
 * The catch-all BFF proxy (FRONTEND-BFF §5).
 *
 * The browser talks only to this origin; this route is what reaches the estate.
 * Everything the guide requires of it is here: prefix -> backend routing, the
 * candidate ladder with 403 treated as "wrong ingress, try the next rung", 503
 * only when every candidate failed, streamed bodies rather than buffered ones,
 * and a timeout sized for a scale-to-zero cold start (P7).
 *
 * There is no bearer injection, because this system has no user accounts
 * (ADR-0004). When it gains them, the token is read from the HttpOnly cookie
 * here, server-side, and client JavaScript still never sees it.
 */

export const dynamic = "force-dynamic";

/**
 * Generous on purpose. A scaled-to-zero callee has to boot before it answers,
 * and a timeout shorter than that cold start is a guaranteed failure that looks
 * like a bug in the callee (FLY-IO §7). The API pins a machine, so this is
 * headroom rather than the primary defence.
 */
const UPSTREAM_TIMEOUT_MS = 30_000;

/** Hop-by-hop headers, plus the ones the fetch layer must set itself. */
const STRIPPED_REQUEST_HEADERS = new Set([
  "host", "connection", "keep-alive", "transfer-encoding", "upgrade",
  "proxy-authorization", "proxy-authenticate", "te", "trailer", "content-length",
]);

const STRIPPED_RESPONSE_HEADERS = new Set([
  "connection", "keep-alive", "transfer-encoding", "upgrade", "content-encoding",
]);

async function proxy(request: NextRequest, context: { params: Promise<{ path: string[] }> }) {
  const { path } = await context.params;
  const backend = backendFor(path);
  const suffix = path.join("/");
  const search = request.nextUrl.search;

  const headers = new Headers();
  request.headers.forEach((value, key) => {
    if (!STRIPPED_REQUEST_HEADERS.has(key.toLowerCase())) {
      headers.set(key, value);
    }
  });

  // Read the body once: it cannot be replayed across candidates otherwise.
  const body = request.method === "GET" || request.method === "HEAD"
    ? undefined
    : await request.arrayBuffer();

  const failures: string[] = [];

  for (const candidate of candidatesFor(backend)) {
    const target = `${candidate}/${suffix}${search}`;
    const abort = new AbortController();
    const timer = setTimeout(() => abort.abort(), UPSTREAM_TIMEOUT_MS);

    try {
      const upstream = await fetch(target, {
        method: request.method,
        headers,
        body,
        signal: abort.signal,
        // A 3xx between services is always a configuration bug, and following it
        // silently converts a POST into a GET so a create "succeeds" against the
        // list endpoint (SERVICE-API-PATTERNS §5).
        redirect: "manual",
        cache: "no-store",
      });

      if (upstream.status >= 300 && upstream.status < 400) {
        failures.push(`${target} -> ${upstream.status} redirect to ${upstream.headers.get("location")}`);
        return NextResponse.json(
          {
            error: "upstream_redirect",
            message: "The backend answered with a redirect. A redirect between services is a configuration bug, not a route.",
          },
          { status: 502 },
        );
      }

      // 403 here means we reached something that is not our backend — a shared
      // ingress, usually. Try the next rung rather than surfacing it.
      if (upstream.status === 403) {
        failures.push(`${target} -> 403 (wrong ingress)`);
        continue;
      }

      const responseHeaders = new Headers();
      upstream.headers.forEach((value, key) => {
        if (!STRIPPED_RESPONSE_HEADERS.has(key.toLowerCase())) {
          responseHeaders.set(key, value);
        }
      });

      // Streamed, not buffered: a download must not be held in the route's
      // memory before the first byte reaches the browser.
      return new NextResponse(upstream.body, {
        status: upstream.status,
        statusText: upstream.statusText,
        headers: responseHeaders,
      });
    } catch (error) {
      const reason = error instanceof Error && error.name === "AbortError"
        ? `timed out after ${UPSTREAM_TIMEOUT_MS}ms`
        : String(error);
      failures.push(`${target} -> ${reason}`);
    } finally {
      clearTimeout(timer);
    }
  }

  // Only once every rung has failed. Logged server-side with the addresses
  // tried, because "503" alone tells whoever is on call nothing.
  console.error(`[proxy] every candidate for '${backend}' failed:\n  ${failures.join("\n  ")}`);

  return NextResponse.json(
    {
      error: "upstream_unavailable",
      message: `No candidate address for '${backend}' answered.`,
    },
    { status: 503 },
  );
}

export const GET = proxy;
export const POST = proxy;
export const PUT = proxy;
export const PATCH = proxy;
export const DELETE = proxy;
export const HEAD = proxy;
