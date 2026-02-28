# Nupeek

Nupeek is a CLI that decompiles **specific NuGet types** into local readable C# so AI coding agents can reason from real implementation details.

## The problem

AI coding agents can use methods from **NuGet packages**, but they often can’t inspect package implementation fast enough.

That causes:
- generic advice instead of behavior-accurate guidance
- slower debugging when edge cases happen in package internals
- risky refactors based on assumptions

Examples:
- “Why is `ServiceBusSender` failing on retries?”
- “Is this Polly path safe to retry or can it duplicate side effects?”
- “What changed in this package behavior after version upgrade?”

## The solution

Nupeek decompiles only the type you care about and writes it locally to `deps-src/`, plus lookup catalogs (`index.json`, `manifest.json`).

So agents can reason from actual implementation details instead of guesswork.

## Quick example

```bash
nupeek type --package Azure.Messaging.ServiceBus \
  --type Azure.Messaging.ServiceBus.ServiceBusSender \
  --out deps-src --dry-run false --progress auto

# machine-readable contract for agents/tools
nupeek type --package Polly --type Polly.Policy --out deps-src --format json
```

Output goes to:
- `deps-src/packages/.../*.decompiled.cs`
- `deps-src/index.json`
- `deps-src/manifest.json`

## Install

See: **`docs/INSTALL.md`**

## Learn more

- Deep project/reference doc: **`docs/README_DEEP.md`**
- Release process: `docs/RELEASE.md`
- Hosting (GitHub Pages + DNS): `docs/HOSTING.md`
- CLI UX standards: `docs/CLI_BEST_PRACTICES.md`
- Landing page: `web/index.html`
