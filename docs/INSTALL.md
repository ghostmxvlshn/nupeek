# Install Nupeek

## Prerequisites

- .NET SDK 10+

Verify:

```bash
dotnet --list-sdks
```

## Option 1: Global install (recommended)

```bash
dotnet tool install -g Nupeek
```

### One-line installer (curl | sh)

```bash
curl -fsSL https://raw.githubusercontent.com/ghostmxvlshn/nupeek/main/install.sh | bash
```

Pin a version:

```bash
curl -fsSL https://raw.githubusercontent.com/ghostmxvlshn/nupeek/main/install.sh | VERSION=0.1.0 bash
```

Update later:

```bash
dotnet tool update -g Nupeek
```

Verify:

```bash
nupeek --help
```

## Option 2: Local (repo-scoped) tool install

```bash
dotnet new tool-manifest
# Package name and command name may differ; command remains `nupeek`
dotnet tool install Nupeek
```

Restore in CI/other machines:

```bash
dotnet tool restore
```

Run:

```bash
dotnet nupeek --help
```

## Quick smoke test

```bash
nupeek type \
  --package Azure.Messaging.ServiceBus \
  --type Azure.Messaging.ServiceBus.ServiceBusSender \
  --out deps-src \
  --dry-run false
```

Expected outputs:
- generated source under `deps-src/packages/...`
- `deps-src/index.json`
- `deps-src/manifest.json`
