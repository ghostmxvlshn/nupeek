#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cd "$repo_root"

git config core.hooksPath .githooks
chmod +x .githooks/pre-commit

echo "Git hooks installed. pre-commit will run dotnet format and dotnet test."
