import { expect, test } from "@playwright/test";

/**
 * The one journey this template ships, and it is a real one: it fails if any
 * link in the chain breaks — the page, the BFF proxy, the API, the schema, or
 * the boot recorder.
 *
 * Every assertion here is unconditional. No guard-then-bail
 * (`if (count === 0) return`), no swallowed failure, no placeholder — the three
 * patterns that make a suite report green while testing nothing
 * (E2E-ACCEPTANCE-TESTING §2). Locators are role- and accessible-name-based,
 * with `data-testid` used only where no accessible name reaches the element (§3).
 */

test.describe("the boot log", () => {
  test("@smoke the page renders and reports the API's recorded starts", async ({ page }) => {
    await page.goto("/");

    await expect(
      page.getByRole("heading", { name: "architecture-standards-init-example", level: 1 }),
    ).toBeVisible();

    // The table only renders when the server-side fetch reached the API and the
    // API returned rows — which means the schema migrated and the boot recorder
    // wrote one. Asserting it is visible asserts the whole path.
    const bootLog = page.getByTestId("boot-log");
    await expect(bootLog).toBeVisible();

    // At least the row for the start that is serving this very request.
    const rows = bootLog.getByRole("row");
    await expect(rows.nth(1)).toBeVisible();

    // The provider is reported, and with no credentials configured it is the
    // in-memory one — the visible, page-level proof of P8's degradation.
    await expect(rows.nth(1)).toContainText("InMemory");
  });

  test("@smoke the runtime config route answers with values read at request time", async ({ request }) => {
    const response = await request.get("/api/config");
    expect(response.status()).toBe(200);

    const config = await response.json();

    // `e2e` is set on the server process by playwright.config.ts. Reading it
    // back proves the value came from the environment at request time rather
    // than from the bundle at build time — the failure this route exists to
    // prevent is a staging frontend calling the production API.
    expect(config.environment).toBe("e2e");

    // The browser is never handed a backend address.
    expect(config.apiBasePath).toBe("/api/proxy");
    expect(JSON.stringify(config)).not.toContain("http://");
  });

  test("@smoke the BFF proxy reaches the API from the browser's own origin", async ({ request }) => {
    const response = await request.get("/api/proxy/api/boots?limit=2000000");
    expect(response.status()).toBe(200);

    const page = await response.json();

    // Proxied through, and the API's clamp survived the hop: a limit of two
    // million comes back as the maximum page size, not as two million rows.
    expect(page.limit).toBe(100);
    expect(Array.isArray(page.items)).toBe(true);
    expect(page.items.length).toBeGreaterThan(0);
  });

  test("an unknown boot record returns the estate's uniform error shape through the proxy", async ({ request }) => {
    const response = await request.get("/api/proxy/api/boots/00000000-0000-0000-0000-000000000000");
    expect(response.status()).toBe(404);

    const body = await response.json();
    expect(body.error).toBe("not_found");
    expect(typeof body.message).toBe("string");
    expect(body.message.length).toBeGreaterThan(0);
  });
});
