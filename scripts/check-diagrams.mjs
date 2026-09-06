#!/usr/bin/env node
/**
 * A diagram has ONE source (RESEARCH-DOCUMENTATION, "Diagrams in a PDF").
 *
 * The problem this solves: GitHub renders only *inline* mermaid, and a PDF
 * renders only a *file*. So the same diagram has to appear in two places, and two
 * copies of anything drift. The wrong answer is redrawing it in TikZ for the PDF;
 * the right answer is one source plus a check that the copies agree.
 *
 * The join is by id, not by filename: a section headed `### A1. Anything` is
 * matched to the file whose name starts `a1-`, so filenames stay free to describe
 * the diagram while the id does the joining.
 *
 *   node scripts/check-diagrams.mjs
 *
 * Exits non-zero and prints a diff when a fenced ```mermaid block does not match
 * its .mmd file byte for byte, ignoring only trailing whitespace per line.
 */

import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";

const DIAGRAM_DIR = "docs/diagrams";

/** Files that embed a diagram inline, and must agree with the source. */
const EMBEDDERS = [
  { file: "docs/architecture/00-ARCHITECTURE.md", id: "a1" },
];

const normalize = (text) =>
  text
    .replace(/\r\n/g, "\n")
    .split("\n")
    .map((line) => line.replace(/\s+$/, ""))
    .join("\n")
    .trim();

const sources = new Map();
for (const name of readdirSync(DIAGRAM_DIR)) {
  if (!name.endsWith(".mmd")) continue;
  const id = name.split("-")[0];
  sources.set(id, { name, body: normalize(readFileSync(join(DIAGRAM_DIR, name), "utf8")) });
}

if (sources.size === 0) {
  console.error(`No .mmd files found in ${DIAGRAM_DIR}.`);
  process.exit(1);
}

let failures = 0;

for (const { file, id } of EMBEDDERS) {
  const source = sources.get(id);
  if (!source) {
    console.error(`FAIL ${file}: no diagram source with id '${id}' in ${DIAGRAM_DIR}.`);
    failures++;
    continue;
  }

  const markdown = readFileSync(file, "utf8").replace(/\r\n/g, "\n");
  const blocks = [...markdown.matchAll(/```mermaid\n([\s\S]*?)```/g)].map((m) => normalize(m[1]));

  if (blocks.length === 0) {
    console.error(`FAIL ${file}: expected an inline mermaid block matching ${source.name}, found none.`);
    failures++;
    continue;
  }

  if (!blocks.includes(source.body)) {
    console.error(`FAIL ${file}: no inline mermaid block matches ${DIAGRAM_DIR}/${source.name}.`);
    console.error("     The file is the source. Copy it into the fenced block, or edit the file and re-copy.");
    console.error("--- expected (from the source file) ---");
    console.error(source.body);
    console.error("--- found (first inline block) ---");
    console.error(blocks[0]);
    failures++;
    continue;
  }

  console.log(`OK   ${file} matches ${DIAGRAM_DIR}/${source.name}`);
}

const unembedded = [...sources.entries()]
  .filter(([id]) => !EMBEDDERS.some((e) => e.id === id))
  .map(([, s]) => s.name);

if (unembedded.length > 0) {
  // Not a failure: a diagram may exist only for the PDF. Worth saying, so a
  // diagram nobody can see on GitHub is a choice rather than an accident.
  console.log(`note: rendered for the PDF only, not embedded in any markdown: ${unembedded.join(", ")}`);
}

process.exit(failures === 0 ? 0 : 1);
