# Nupeek

Nupeek is a CLI that decompiles **specific NuGet types** into local readable C# so AI coding agents can reason from real implementation details.

## Why Nupeek

Agents often call package APIs without seeing internals quickly. Nupeek fills that gap with targeted decompilation (type-level, not full dependency dumps), so debugging and code suggestions are more accurate.

## Quick example

```bash
nupeek type --package Azure.Messaging.ServiceBus \
  --type Azure.Messaging.ServiceBus.ServiceBusSender \
  --out deps-src --dry-run false
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
- CLI UX standards: `docs/CLI_BEST_PRACTICES.md`
- Landing page: `web/index.html`
