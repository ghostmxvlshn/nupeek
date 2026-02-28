# Nupeek (Deep Reference)

## What Nupeek does

Nupeek decompiles targeted types from NuGet packages into a local `deps-src/` folder, making internals visible to coding agents (Copilot CLI, Claude Code, Codex, etc.).

## Why this project exists

Without source visibility, agents often infer behavior from signatures/docs only. Nupeek improves accuracy by giving them actual implementation output for the exact types you use.

## Core commands

```bash
nupeek type --package <id> [--version <v>] [--tfm <tfm>] --type "<Namespace.Type>" --out deps-src [--format text|json] [--progress auto|always|never]
nupeek find --package <id> [--version <v>] [--tfm <tfm>] --symbol "<Namespace.Type.Method>" --out deps-src [--format text|json] [--progress auto|always|never]
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

## Notes

- Generated sources are local artifacts; avoid redistributing decompiled output.
- Nupeek is optimized for targeted inspection, not whole ecosystem dumps.
- Use `index.json` and `manifest.json` for deterministic lookup/provenance.

## Related docs

- Install guide: `docs/INSTALL.md`
- Release guide: `docs/RELEASE.md`
- CLI best practices: `docs/CLI_BEST_PRACTICES.md`
