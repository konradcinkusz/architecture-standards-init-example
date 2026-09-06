import { backendFor, candidatesFor } from "@/lib/backends";

/**
 * The one screen this template ships. It renders the API's boot log and its
 * integration report, which makes it the cheapest end-to-end check there is:
 * if this page shows a row, then the frontend reached the BFF proxy, the proxy
 * reached the API, the API reached its database, and the schema really migrated.
 *
 * The fetch happens server-side, so the browser makes no cross-origin call and
 * learns no backend address (FRONTEND-BFF §1).
 */

export const dynamic = "force-dynamic";

type BootRecord = {
  id: string;
  recordedAt: string;
  version: string;
  environment: string;
  databaseProvider: string;
  degradedIntegrations: string[];
};

type Page = { items: BootRecord[]; total: number };

async function loadBoots(): Promise<{ page: Page | null; error: string | null }> {
  for (const candidate of candidatesFor(backendFor(["api"]))) {
    try {
      const response = await fetch(`${candidate}/api/boots?limit=10`, { cache: "no-store" });
      if (response.ok) {
        return { page: (await response.json()) as Page, error: null };
      }
    } catch {
      // Fall through to the next rung; the ladder is the point.
    }
  }
  return { page: null, error: "No candidate address for the API answered." };
}

export default async function Home() {
  const { page, error } = await loadBoots();

  return (
    <main>
      <h1>architecture-standards-init-example</h1>
      <p className="lede">
        One Aspire composition root, one shared kernel, one service that owns its database,
        and this Next.js surface in front of it.
      </p>

      <h2>Recorded starts of the API</h2>
      <p>
        One row per start, written by a hosted service after the schema migration completes.
        A row here means the whole path worked: this page &rarr; <code>/api/proxy</code> &rarr;
        the API &rarr; <code>apidb</code>.
      </p>

      {error && <p className="empty">{error}</p>}

      {page && page.items.length === 0 && (
        <p className="empty">No starts recorded yet.</p>
      )}

      {page && page.items.length > 0 && (
        <div className="scroll">
          {/* data-testid, used deliberately: a table has no accessible name to
              locate it by, and the E2E suite needs a stable handle for the one
              element that proves the whole path worked (E2E §3). */}
          <table data-testid="boot-log">
            <thead>
              <tr>
                <th>Recorded</th>
                <th>Version</th>
                <th>Environment</th>
                <th>Provider</th>
                <th>Optional integrations</th>
              </tr>
            </thead>
            <tbody>
              {page.items.map((boot) => (
                <tr key={boot.id}>
                  <td>{new Date(boot.recordedAt).toISOString().replace("T", " ").slice(0, 19)}</td>
                  <td><code>{boot.version}</code></td>
                  <td>{boot.environment}</td>
                  <td>{boot.databaseProvider}</td>
                  <td>
                    {boot.degradedIntegrations.length === 0 ? (
                      <span className="state-ok">all configured</span>
                    ) : (
                      <span className="state-degraded">
                        degraded: {boot.degradedIntegrations.join(", ")}
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  );
}
