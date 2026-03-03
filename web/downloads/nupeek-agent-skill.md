---
name: nupeek
description: "Targeted NuGet decompilation for coding agents. Use when an AI agent needs real implementation details from a NuGet package type (retry behavior, guards, internals, version differences) before proposing or applying code changes."
---

# Nupeek Skill

Use Nupeek to give the agent real package implementation context.

## Trigger Conditions

Use this skill when the task depends on behavior inside a NuGet dependency and source is not already available.

Examples:
- Understand why a package API behaves unexpectedly
- Verify retry, timeout, guard, or fallback logic in dependency code
- Compare type behavior across package versions
- Confirm assumptions before refactoring integration code

Skip this skill when:
- The issue is in your own source code only
- Public docs already answer the question with enough confidence
- You do not need internals to make a decision

## Workflow

1. Resolve source input. Prefer **assembly mode** when possible (this is usually best in real projects):
   - if the dependency DLL already exists in `bin/`, `obj/`, `.nuget/packages`, or project artifacts, use `--assembly`
   - fall back to `--package` only when assembly path is unavailable

2. If the user asks about a **method implementation**, inspect local code usage first:
   - find where the method is called in the current codebase
   - use surrounding type names/usings to infer the likely declaring type
   - prefer decompiling that concrete type directly

```bash
# Example: derive likely type from usage in your codebase first
rg -n "WaitAndRetry\(" .
```

3. Run Nupeek for the exact type first (narrow scope), preferring assembly input:

```bash
# Preferred in ~99% of real tasks (assembly already present locally)
nupeek type --assembly <path-to.dll> --type <Namespace.Type> --out deps-src

# Include directly related types (base + interfaces)
nupeek type --assembly <path-to.dll> --type <Namespace.Type> --depth 1 --out deps-src

# Export full structural graph for agents (provider-only)
nupeek graph --assembly <path-to.dll> --type <Namespace.Type> --depth 2 --out deps-src

# Fallback when assembly path is not available
nupeek type --package <PackageId> --type <Namespace.Type> --out deps-src
```

4. If the type is unknown, discover candidates from member/type symbol:

```bash
# Preferred
nupeek find --assembly <path-to.dll> --symbol <Namespace.TypeOrMember> --out deps-src

# Fallback
nupeek find --package <PackageId> --symbol <Namespace.TypeOrMember> --out deps-src
```

5. If `find` returns multiple candidates for a member, choose one declaring type and rerun `type`:

```bash
nupeek type --assembly <path-to.dll> --type <Resolved.Namespace.Type> --out deps-src
```

6. Use file-first workflow only (Nupeek writes artifacts to disk).

Example:

```bash
nupeek type --package <PackageId> --type <Namespace.Type> --out deps-src
```

7. Inspect generated files under `deps-src/`:
- Decompiled source file(s)
- `index.json` for quick navigation
- `manifest.json` for run metadata
- Graph exports from `nupeek graph`:
  - `graph.types.json` → type nodes (name/full name/base/interfaces)
  - `graph.members.json` → methods/properties/fields/events per type
  - `graph.edges.json` → structural relations (`inherits`, `implements`)
  - `graph.globals.json` → static fields/constants (global-like state)

Example graph command and output shape:

```bash
nupeek graph --assembly <path-to.dll> --type <Namespace.Type> --depth 2 --out deps-src
```

```json
// graph.types.json
[{ "Name": "RetryStrategyOptions`1", "FullName": "Polly.Retry.RetryStrategyOptions`1", "BaseType": "Polly.ResilienceStrategyOptions", "Interfaces": [] }]

// graph.members.json
[{ "DeclaringType": "Polly.Retry.RetryStrategyOptions`1", "Kind": "property", "Name": "MaxRetryAttempts", "IsStatic": false, "Visibility": "unknown" }]

// graph.edges.json
[{ "FromType": "Polly.Retry.RetryStrategyOptions`1", "Relation": "inherits", "ToType": "Polly.ResilienceStrategyOptions" }]
```

8. Base guidance/patches on observed implementation, not API guesswork.

## Output Contract

When using Nupeek, report:
- exact package + version inspected
- target framework / assembly selected
- file path(s) used for evidence
- concise conclusion tied to observed code

## CLI Options to Use Intentionally

- `--assembly <path-to.dll>` → preferred source when dependency assembly already exists locally
- `--package <id>` (+ optional `--version`) → fallback source when assembly path is unavailable
- `--depth <n>` → include related types (base/interfaces) when decompiling
- `graph` command → export `graph.types/members/edges/globals.json` for agent consumption (no analysis)
- `--progress auto|always|never` → terminal spinner behavior
- `--dry-run` → show execution plan without decompiling

## Safety + Scope

- Prefer `nupeek type` over broad extraction.
- Keep output local to workspace (`deps-src/`).
- Do not claim behavior not visible in the extracted code.
