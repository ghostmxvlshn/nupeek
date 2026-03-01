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

1. Resolve package name + version from project files/lock data.

2. If the user asks about a **method implementation**, inspect local code usage first:
   - find where the method is called in the current codebase
   - use surrounding type names/usings to infer the likely declaring type
   - prefer decompiling that concrete type directly

```bash
# Example: derive likely type from usage in your codebase first
rg -n "WaitAndRetry\(" .
```

3. Run Nupeek for the exact type first (narrow scope):

```bash
nupeek type --package <PackageId> --type <Namespace.Type> --out deps-src
```

4. If the type is unknown, discover candidates from member/type symbol:

```bash
nupeek find --package <PackageId> --symbol <Namespace.TypeOrMember> --out deps-src
```

5. If `find` returns multiple candidates for a member, choose one declaring type and rerun `type`:

```bash
nupeek type --package <PackageId> --type <Resolved.Namespace.Type> --out deps-src
```

6. Choose output mode based on task:
- **Default (`--emit files`)** when you want reproducible local artifacts and can read files from disk.
- **Agent fast path (`--emit agent --format json`)** when you want inline source + metadata immediately in command output.

Examples:

```bash
# Reproducible files-first mode
nupeek type --package <PackageId> --type <Namespace.Type> --out deps-src --emit files

# Agent-ready inline mode (no extra file-read step)
nupeek type --package <PackageId> --type <Namespace.Type> --out deps-src --format json --emit agent --max-chars 12000
```

7. Inspect generated files under `deps-src/` when using files mode:
- Decompiled source file(s)
- `index.json` for quick navigation
- `manifest.json` for run metadata

8. Base guidance/patches on observed implementation, not API guesswork.

## Output Contract

When using Nupeek, report:
- exact package + version inspected
- target framework / assembly selected
- file path(s) used for evidence
- concise conclusion tied to observed code

## CLI Options to Use Intentionally

- `--format text|json` → human vs machine-readable output
- `--emit files|agent` → files-first vs inline agent payload
- `--max-chars <n>` → inline source cap for `--emit agent`
- `--progress auto|always|never` → terminal spinner behavior
- `--dry-run` → show execution plan without decompiling

## Safety + Scope

- Prefer `nupeek type` over broad extraction.
- Keep output local to workspace (`deps-src/`).
- Do not claim behavior not visible in the extracted code.
