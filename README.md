# Nupeek

Nupeek is a CLI that decompiles **specific NuGet types** into local readable C# so AI coding agents can reason from real implementation details.

## Why this project exists

When agents use NuGet APIs, they usually don’t see the source behind them quickly. That causes:
- wrong assumptions about behavior
- slower debugging
- weaker refactoring suggestions

Nupeek solves this by generating only what you need (type-level), not a massive full dependency dump.

## How it helps (real examples)

- **Debug behavior differences**
  - “Why does `ServiceBusSender` behave this way?”
  - Decompile that exact type and inspect implementation.

- **Write safer integration code**
  - Agent can inspect retry/null/exception paths in package internals.

- **Improve AI answers quality**
  - Instead of guessing from docs, your agent reads `deps-src/*.decompiled.cs`.

## Core commands

```bash
nupeek type --package <id> [--version <v>] [--tfm <tfm>] --type "<Namespace.Type>" --out deps-src
nupeek find --package <id> [--version <v>] [--tfm <tfm>] --symbol "<Namespace.Type.Method>" --out deps-src
```

## Typical output

```
deps-src/
  packages/<id>/<version>/<tfm>/
    <TypeName>.decompiled.cs
  index.json
  manifest.json
```

## Quick start

See install + usage docs: **`docs/INSTALL.md`**

## Project docs

- Install: `docs/INSTALL.md`
- Release: `docs/RELEASE.md`
- CLI UX standards: `docs/CLI_BEST_PRACTICES.md`
- Landing page: `web/index.html`

## Dev

```bash
./scripts/install-hooks.sh
```

Hooks:
- `pre-commit`: format + tests
- `commit-msg`: icon + Conventional Commit format
