#!/usr/bin/env node
/**
 * Every relative link in the documentation resolves to a file that exists.
 *
 *   node scripts/check-links.mjs
 *
 * This used to be a shell loop inlined in ci.yml, and it never once ran. The
 * step was `shell: bash -e` and the script began `set -euo pipefail`, so the
 * first markdown file containing no links made `grep` exit 1, `pipefail`
 * propagated it, and `-e` killed the script before it checked anything — exit 1,
 * no output, indistinguishable from "found broken links".
 *
 * It failed closed, which is the safe direction, but it was still a gate
 * reporting a verdict it had not earned. It lives here now because a file can be
 * run identically on a laptop and in CI, on Windows and on Linux, and because
 * shell-quoting traps do not survive being written in a language with arrays.
 */

import { readFileSync, existsSync, statSync, readdirSync } from "node:fs";
import { join, dirname, resolve, relative, sep } from "node:path";

const ROOT = process.cwd();

/** Directories never worth walking; node_modules dwarfs everything else. */
const SKIP = new Set(["node_modules", ".git", ".next", "bin", "obj", "rendered", "test-results", "playwright-report"]);

function markdownFiles(dir) {
  const found = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.isDirectory()) {
      if (SKIP.has(entry.name)) continue;
      found.push(...markdownFiles(join(dir, entry.name)));
    } else if (entry.name.endsWith(".md")) {
      found.push(join(dir, entry.name));
    }
  }
  return found;
}

/**
 * Code is not prose, and a link inside it is not a link. Fenced blocks and
 * inline spans are blanked before scanning — line structure preserved, so error
 * output still points at the right place.
 *
 * This is not hypothetical tidiness: `flyio/SECRETS.md` documents a PowerShell
 * one-liner beginning `[System.Convert]::ToHexString(…)`, which is
 * indistinguishable from a markdown reference definition (`[label]: target`)
 * unless you know it is inside a fence.
 */
function stripCode(markdown) {
  return markdown
    .replace(/^([ \t]*)(`{3,}|~{3,})[^\n]*\n[\s\S]*?^\1?\2[^\n]*$/gm, (block) =>
      block.replace(/[^\n]/g, " "))
    .replace(/`[^`\n]*`/g, (span) => " ".repeat(span.length));
}

/**
 * Inline links `[text](target)` and reference definitions `[label]: target`.
 * Anything absolute, protocol-relative or a bare fragment is somebody else's
 * problem — this checks only what is claimed to be in the repository.
 */
function linksIn(markdown) {
  markdown = stripCode(markdown);
  const targets = [];
  const inline = /\[[^\]]*\]\(\s*<?([^)\s>]+)>?(?:\s+"[^"]*")?\s*\)/g;
  const reference = /^\s*\[[^\]]+\]:\s*<?([^\s>]+)>?/gm;

  for (const re of [inline, reference]) {
    let m;
    while ((m = re.exec(markdown)) !== null) targets.push(m[1]);
  }
  return targets;
}

const isExternal = (t) =>
  /^[a-z][a-z0-9+.-]*:/i.test(t) || t.startsWith("//") || t.startsWith("#");

let checked = 0;
const broken = [];

for (const file of markdownFiles(ROOT)) {
  const text = readFileSync(file, "utf8");

  for (const target of linksIn(text)) {
    if (isExternal(target)) continue;

    // Strip the fragment; a path plus an anchor still has to be a real path.
    const path = decodeURIComponent(target.split("#")[0]);
    if (!path) continue;

    checked++;
    const resolved = resolve(dirname(file), path);

    // A link ending in "/" must be a directory; anything else may be either.
    const ok = existsSync(resolved) && (!target.endsWith("/") || statSync(resolved).isDirectory());
    if (!ok) {
      broken.push({ file: relative(ROOT, file).split(sep).join("/"), target });
    }
  }
}

for (const { file, target } of broken) {
  console.error(`::error file=${file}::broken relative link: ${target}`);
  console.error(`FAIL ${file} -> ${target}`);
}

if (broken.length > 0) {
  console.error(`\n${broken.length} broken relative link(s) of ${checked} checked.`);
  process.exit(1);
}

// The count is printed deliberately. A checker that says only "all good" cannot
// be told apart from one that checked nothing — which is the bug this file
// replaces.
console.log(`All ${checked} relative links resolve, across ${markdownFiles(ROOT).length} markdown files.`);
