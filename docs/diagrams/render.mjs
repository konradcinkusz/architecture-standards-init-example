#!/usr/bin/env node
/**
 * Renders every docs/diagrams/*.mmd to a vector PDF in docs/diagrams/rendered/.
 *
 *   pnpm render            all diagrams
 *   pnpm render a1         only the ones whose filename starts with a matching slug
 *
 * Vector, not raster: a .mmd renders straight to PDF, which scales, prints, and
 * keeps its text selectable. A PNG in a paper is a screenshot of a diagram.
 *
 * The output directory is gitignored. Nothing generated is committed — a checked-in
 * artifact disagrees with its source the first time the source changes.
 */

import { spawnSync } from "node:child_process";
import { existsSync, mkdirSync, readdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const outDir = join(here, "rendered");

// Resolved locally, deliberately. `npx mmdc` on a fresh clone whose node_modules
// is still empty reaches past it to the registry and resolves a squatter package
// literally named `mmdc`, then fails with a message that names nothing useful. A
// missing binary should say "run the install".
const binary = join(here, "node_modules", ".bin", process.platform === "win32" ? "mmdc.cmd" : "mmdc");

if (!existsSync(binary)) {
  console.error("mermaid-cli is not installed.\n\n  cd docs/diagrams && pnpm install\n");
  process.exit(1);
}

const filters = process.argv.slice(2);
const sources = readdirSync(here)
  .filter((name) => name.endsWith(".mmd"))
  .filter((name) => filters.length === 0 || filters.some((f) => name.startsWith(f)));

if (sources.length === 0) {
  console.error(filters.length ? `No .mmd file matches: ${filters.join(", ")}` : "No .mmd files found.");
  process.exit(1);
}

mkdirSync(outDir, { recursive: true });

let failures = 0;
for (const source of sources) {
  const slug = source.replace(/\.mmd$/, "");
  const output = join(outDir, `${slug}.pdf`);
  process.stdout.write(`rendering ${slug} ... `);

  const result = spawnSync(
    binary,
    [
      "-i", join(here, source),
      "-o", output,
      "--pdfFit",
      "-b", "transparent",
      // The renderer drives a headless Chromium; CI and dev containers commonly
      // run as root, where its sandbox refuses to start.
      "--puppeteerConfigFile", join(here, "puppeteer.json"),
    ],
    { stdio: ["ignore", "pipe", "pipe"], shell: process.platform === "win32" },
  );

  if (result.status === 0) {
    console.log("ok");
  } else {
    console.log("FAILED");
    console.error(result.stderr?.toString() ?? "");
    failures++;
  }
}

process.exit(failures === 0 ? 0 : 1);
