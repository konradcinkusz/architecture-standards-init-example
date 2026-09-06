import { defineConfig, devices } from "@playwright/test";

/**
 * The E2E harness. Every entry point this repository claims is executed by some
 * CI context (TESTING-STRATEGY §9): this config is run by the `e2e` job in
 * ci.yml on every pull request. An unreferenced test config is not a latent
 * capability, it is documentation that lies.
 *
 * Booting the stack here rather than pointing at a deployed environment is a
 * deliberate, and unusually cheap, exception to E2E-ACCEPTANCE-TESTING §6: this
 * system is two processes, and P8 guarantees both start with zero credentials.
 * When a PR-preview environment exists, this points at its URL instead.
 */

const CI = !!process.env.CI;
const WEB_PORT = process.env.E2E_WEB_PORT ?? "3100";
const API_PORT = process.env.E2E_API_PORT ?? "5180";
const BASE_URL = process.env.E2E_BASE_URL ?? `http://127.0.0.1:${WEB_PORT}`;

export default defineConfig({
  testDir: "./tests",
  fullyParallel: true,
  forbidOnly: CI,
  retries: CI ? 2 : 0,
  workers: CI ? 1 : undefined,
  reporter: CI ? [["github"], ["html", { open: "never" }]] : [["list"]],

  use: {
    baseURL: BASE_URL,
    trace: "on-first-retry",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
  },

  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"] } },
  ],

  // Started only when E2E_BASE_URL is unset — with it set, the suite runs
  // against something already deployed and must not start anything.
  webServer: process.env.E2E_BASE_URL
    ? undefined
    : [
        {
          // No connection string, no credentials: the API falls back to the
          // in-memory provider, which is exactly what P8 promises.
          command: `dotnet run --project ../../src/ArchitectureStandardsInitExample.Api --urls http://127.0.0.1:${API_PORT}`,
          url: `http://127.0.0.1:${API_PORT}/health`,
          reuseExistingServer: !CI,
          timeout: 120_000,
        },
        {
          command: "node .next/standalone/app/server.js",
          cwd: "../../web/app",
          url: `http://127.0.0.1:${WEB_PORT}/healthz`,
          reuseExistingServer: !CI,
          timeout: 120_000,
          env: {
            PORT: WEB_PORT,
            HOSTNAME: "127.0.0.1",
            API_BASE_URL: `http://127.0.0.1:${API_PORT}`,
            APP_ENVIRONMENT: "e2e",
          },
        },
      ],
});
