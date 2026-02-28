# Nupeek

Targeted NuGet decompilation for coding agents (Copilot CLI / Claude Code).

## Vision
When a developer calls a method from a NuGet package, generate readable decompiled C# for the specific type into `deps-src/` so agents can inspect implementation details on demand.

## Why this project
- Avoid decompiling all transitive dependencies (too noisy/heavy)
- Keep output small and relevant to the exact symbol/type being used
- Cache + index results for fast repeated lookups

## Proposed CLI

```bash
nupeek type --package <id> [--version <v>] [--tfm <tfm>] --type "<Namespace.Type>" --out deps-src
nupeek find --package <id> [--version <v>] [--tfm <tfm>] --symbol "<Namespace.Type.Method>" --out deps-src
nupeek dump --package <id> [--version <v>] [--tfm <tfm>] --out deps-src
```

## Output contract

```
deps-src/
  packages/<id>/<version>/<tfm>/
    <TypeName>.decompiled.cs
  index.json
  manifest.json
  README.md
```

## MVP scope
1. `nupeek type` and `nupeek find`
2. Download/extract `.nupkg`
3. Pick target `lib/<tfm>/`
4. Scan DLL metadata to find assembly containing target type
5. Decompile only that type via ILSpy engine
6. Write output + update `index.json` and `manifest.json`

## Tech stack (initial)
- .NET 10 console app
- `ICSharpCode.Decompiler`
- `NuGet.Protocol`
- `NuGet.Packaging`
- `NuGet.Configuration`

## Notes
- Keep generated sources local; avoid redistributing decompiled output.
- Prefer symlink or local `deps-src/` under repo for agent visibility.

## Open decisions
- Package selection strategy for multi-dll packages (auto vs `--assembly` override)
- TFM auto-selection heuristic defaults

## Dev
- Runtime: .NET 10 (`net10.0`)
- CI: `.github/workflows/ci.yml` (restore/build/test)
- Planning: `PLAN.md`
- CLI UX standards: `docs/CLI_BEST_PRACTICES.md` (derived from https://clig.dev)
- C# style rules: `.editorconfig`

## Git hooks (recommended)
Install local hooks once per clone:

```bash
./scripts/install-hooks.sh
```

Enabled pre-commit checks:
- `dotnet format Nupeek.slnx --verify-no-changes`
- `dotnet test Nupeek.slnx --configuration Release`
