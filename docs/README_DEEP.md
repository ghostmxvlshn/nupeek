# Nupeek (Deep Reference)

## What Nupeek does

Nupeek decompiles targeted types from NuGet packages into a local `deps-src/` folder, making internals visible to coding agents (Copilot CLI, Claude Code, Codex, etc.).

## Why this project exists

Without source visibility, agents often infer behavior from signatures/docs only. Nupeek improves accuracy by giving them actual implementation output for the exact types you use.

## Core commands

```bash
nupeek type --package <id> [--version <v>] [--tfm <tfm>] --type "<Namespace.Type>" --out deps-src [--depth <n>] [--progress auto|always|never]
nupeek find --package <id> [--version <v>] [--tfm <tfm>] --symbol "<Namespace.Type.Method>" --out deps-src [--progress auto|always|never]
nupeek graph --assembly <path-to.dll> [--type "<Namespace.Type>"] [--depth <n>] --out deps-src
```

## Typical output layout

```
deps-src/
  .cache/
    packages/<id>/<version>/<id>.<version>.nupkg
  packages/<id>/<version>/<tfm>/
    <TypeName>.decompiled.cs
  index.json
  manifest.json
```

## Practical examples

### Example 1: inspect ServiceBus sender internals

```bash
nupeek type --package Azure.Messaging.ServiceBus \
  --type Azure.Messaging.ServiceBus.ServiceBusSender \
  --out deps-src --dry-run false
```

### Example 2: resolve symbol to type then decompile

```bash
nupeek find --package Polly \
  --symbol Polly.Policy.Handle \
  --out deps-src --dry-run false
```

## Graph output files (for agents)

`nupeek graph` writes four machine-readable files to your `--out` directory:

- `graph.types.json` → type nodes (`Name`, `FullName`, `BaseType`, `Interfaces`)
- `graph.members.json` → member nodes (`DeclaringType`, `Kind`, `Name`, `IsStatic`, `Visibility`)
- `graph.edges.json` → type relations (`inherits`, `implements`)
- `graph.globals.json` → static/global-like fields and constants

Minimal real-world shape:

```json
// graph.types.json
[{ "Name": "ResilienceStrategyOptions", "FullName": "Polly.ResilienceStrategyOptions", "BaseType": "System.Object", "Interfaces": [] }]

// graph.edges.json
[{ "FromType": "Polly.Retry.RetryStrategyOptions`1", "Relation": "inherits", "ToType": "Polly.ResilienceStrategyOptions" }]
```

## Notes

- Generated sources are local artifacts; avoid redistributing decompiled output.
- Nupeek is optimized for targeted inspection, not whole ecosystem dumps.
- Use `index.json`, `manifest.json`, and graph JSON files for deterministic lookup/provenance.

## Related docs

- Install guide: `docs/INSTALL.md`
- Release guide: `docs/RELEASE.md`
- CLI best practices: `docs/CLI_BEST_PRACTICES.md`
