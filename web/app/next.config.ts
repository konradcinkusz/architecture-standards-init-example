import path from "node:path";
import { fileURLToPath } from "node:url";
import type { NextConfig } from "next";

// `new URL("..", import.meta.url).pathname` yields "/C:/..." on Windows, which
// Next cannot canonicalize. fileURLToPath is the portable conversion.
const workspaceRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");

const nextConfig: NextConfig = {
  // P6 — `standalone` emits a self-contained server with only the traced
  // dependencies, which is what the runner stage of the Dockerfile copies.
  output: "standalone",

  // The container's build context is the workspace root (`web/`), so the trace
  // has to start there or the standalone output misses hoisted dependencies.
  outputFileTracingRoot: workspaceRoot,

  // No `env` block, deliberately. Anything environment-specific read at build
  // time is baked into the bundle and costs one image per environment, which
  // breaks build-once-deploy-many (P12, FRONTEND-BFF §2). Addresses come from
  // /api/config at request time instead.
  reactStrictMode: true,
};

export default nextConfig;
