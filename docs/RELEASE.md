# Release Nupeek

## Required repository secrets

- `NUGET_API_KEY` — API key with push permissions for NuGet.org

## Versioning

1. Update package version in `src/Nupeek.Cli/Nupeek.Cli.csproj`.
2. Commit changes.
3. Create and push a tag:

```bash
git tag v0.1.0
git push origin v0.1.0
```

## What happens on tag push

Workflow: `.github/workflows/release.yml`

- restore, build, test
- pack Nupeek global tool package
- publish package to NuGet.org
- create GitHub release and attach `.nupkg`

## Verify published package

```bash
dotnet tool update -g Nupeek
nupeek --help
```
