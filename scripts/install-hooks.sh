#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cd "$repo_root"

git config core.hooksPath .githooks
chmod +x .githooks/pre-commit .githooks/commit-msg

echo "Git hooks installed."
echo "- pre-commit: dotnet format + dotnet test"
echo "- commit-msg: conventional commits + icon format"
