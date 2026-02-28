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
2. Run Nupeek for the exact type first (narrow scope):

```bash
nupeek type --package <PackageId> --type <Namespace.Type> --out deps-src
```

3. If type is unknown, discover candidates:

```bash
nupeek find --package <PackageId> --name <TypeOrPattern> --out deps-src
```

4. Inspect generated files under `deps-src/`:
- Decompiled source file(s)
- `index.json` for quick navigation
- `manifest.json` for run metadata

5. Base guidance/patches on observed implementation, not API guesswork.

## Output Contract

When using Nupeek, report:
- exact package + version inspected
- target framework / assembly selected
- file path(s) used for evidence
- concise conclusion tied to observed code

## Safety + Scope

- Prefer `nupeek type` over broad extraction.
- Keep output local to workspace (`deps-src/`).
- Do not claim behavior not visible in the extracted code.
