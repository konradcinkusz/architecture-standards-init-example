import { NextResponse } from "next/server";

/**
 * A dedicated health endpoint that exists only to be checked (FLY-IO §2).
 *
 * The Fly check must not point at `/`: a frontend serving a broken bundle still
 * returns 200 for its index page, so the check would pass on a white screen.
 * This route answers from the server itself and is deliberately independent of
 * whether any backend is reachable — the frontend being up and the API being up
 * are two different facts, and conflating them makes a cold API look like a
 * failed frontend deploy.
 */
export const dynamic = "force-dynamic";

export async function GET() {
  return NextResponse.json({ status: "healthy" }, { status: 200 });
}
