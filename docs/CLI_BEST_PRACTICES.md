# Nupeek CLI Best Practices (from clig.dev)

This file captures practical CLI UX rules we should enforce in Nupeek.

## 1) Human-first, composable design
- Keep commands understandable for humans first.
- Keep outputs composable for scripts/pipes.
- Design for both interactive usage and automation.

## 2) Command model
- Use subcommands: `nupeek type`, `nupeek find`, later `nupeek dump`.
- Keep naming predictable and consistent.
- Prefer explicit flags over implicit behavior for destructive/expensive operations.

## 3) Argument parsing
- Use a proper parser library (for .NET: `System.CommandLine`).
- Validate required options early and fail with actionable errors.
- Support both short and long flags where appropriate.
- Add typo suggestions when command/flag is unknown.

## 4) Help quality
- `-h`/`--help` at root and each subcommand.
- Running without required args should print concise, useful help.
- Include examples in help text.
- Link to docs for deeper usage.

## 5) stdout vs stderr
- Machine-readable output goes to `stdout`.
- Diagnostics, progress, warnings, and errors go to `stderr`.
- Never pollute JSON output with human text.

## 6) Exit codes
- `0` success.
- Non-zero for known failure modes.
- Start with a small stable mapping:
  - `1` generic/unknown error
  - `2` invalid arguments
  - `3` package resolution failure
  - `4` type/symbol not found
  - `5` decompilation failure

## 7) Errors and recovery guidance
- Error messages should explain what failed + how to fix.
- Include next-step suggestions (`Try --tfm net8.0`, `Check package version`).
- Avoid stack traces by default.
- Add `--verbose` for technical diagnostics.

## 8) Saying just enough
- Default output should be concise and clear.
- Show progress for long operations (download/decompile/index).
- Add `--quiet` and `--verbose` modes.

## 9) Discoverability
- Provide practical examples for common workflows.
- Add suggestion hints after errors.
- Consider shell completion support later.

## 10) Safety and trust
- Add `--dry-run` to show what will happen.
- Confirm when writing outside workspace (future).
- Keep deterministic output layout.

## 11) Determinism and robustness
- Stable file naming and index updates.
- Idempotent behavior where possible (skip if cached unless `--force`).
- Graceful behavior for obfuscated/unreadable assemblies.

## 12) Testing expectations
- Unit tests for parser + validation + path contracts + exit codes.
- Golden tests for help text snapshots.
- Integration tests for `type/find` dry-run and real mode (where feasible).

## 13) CI expectations
- Build + test on every PR/push.
- Enforce formatting/analyzers in CI.
- Add coverage upload when practical.

## 14) MVP checklist
- [ ] Replace custom arg parsing with `System.CommandLine`
- [ ] Implement root and subcommand help with examples
- [ ] Implement stable exit code contract
- [ ] Implement stdout/stderr separation policy
- [ ] Add `--verbose`, `--quiet`, `--dry-run`
- [ ] Add tests for error/help behavior
