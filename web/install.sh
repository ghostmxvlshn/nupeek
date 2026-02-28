#!/usr/bin/env bash
set -euo pipefail

PACKAGE_ID="Nupeek"
VERSION="${VERSION:-}"
TOOLS_PATH="${TOOLS_PATH:-$HOME/.dotnet/tools}"

has_cmd() {
  command -v "$1" >/dev/null 2>&1
}

log() {
  printf '[nupeek-install] %s\n' "$*"
}

if ! has_cmd dotnet; then
  log "dotnet SDK is required but was not found in PATH."
  log "Install .NET SDK 10+ first: https://dotnet.microsoft.com/download"
  exit 1
fi

if dotnet tool list -g | awk 'NR>2 {print $1}' | grep -qi '^nupeek$'; then
  if [[ -n "$VERSION" ]]; then
    log "Updating $PACKAGE_ID to version $VERSION"
    dotnet tool update -g "$PACKAGE_ID" --version "$VERSION"
  else
    log "Updating $PACKAGE_ID to latest"
    dotnet tool update -g "$PACKAGE_ID"
  fi
else
  if [[ -n "$VERSION" ]]; then
    log "Installing $PACKAGE_ID version $VERSION"
    dotnet tool install -g "$PACKAGE_ID" --version "$VERSION"
  else
    log "Installing $PACKAGE_ID latest"
    dotnet tool install -g "$PACKAGE_ID"
  fi
fi

if [[ ":$PATH:" != *":$TOOLS_PATH:"* ]]; then
  log "Nupeek installed, but $TOOLS_PATH is not currently in PATH."
  log "Add this to your shell profile:"
  log "  export PATH=\"\$PATH:$TOOLS_PATH\""
else
  log "PATH already contains $TOOLS_PATH"
fi

if has_cmd nupeek; then
  log "Install complete."
  nupeek --help >/dev/null
  log "Verified: nupeek --help"
else
  log "Nupeek command not visible in current shell yet. Open a new shell or update PATH."
fi
