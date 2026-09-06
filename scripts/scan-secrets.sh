#!/usr/bin/env bash
# Local mirror of the secret-scan CI job (REPO-BASELINE §4), so the job can be
# debugged without pushing. Takes no arguments; scans the whole working tree.
#
#   ./scripts/scan-secrets.sh            scan the working tree
#   ./scripts/scan-secrets.sh --staged   scan only what is staged (what the hook runs)
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

if ! command -v gitleaks >/dev/null 2>&1; then
  cat >&2 <<'MSG'
gitleaks is not installed, so nothing was scanned.

  Windows   winget install gitleaks
  macOS     brew install gitleaks
  Linux     https://github.com/gitleaks/gitleaks/releases

This script exits non-zero rather than passing silently: a scanner that reports
success when it did not run is worse than no scanner (P5).
MSG
  exit 127
fi

if [[ "${1:-}" == "--staged" ]]; then
  echo "Scanning staged changes..."
  gitleaks protect --staged --redact --config .gitleaks.toml --verbose
else
  echo "Scanning the working tree and its history..."
  gitleaks detect --redact --config .gitleaks.toml --verbose
fi

echo "No secrets found."
