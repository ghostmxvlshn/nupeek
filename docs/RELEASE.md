# Release Nupeek

## Required repository secrets

- `NUGET_API_KEY` — API key with push permissions for NuGet.org

## Automatic release model

Nupeek now releases automatically on every push to `main`.

Workflow: `.github/workflows/release.yml`

What it does on each `main` push:

1. restore, build, test
2. compute package version from:
   - `<VersionPrefix>` in `src/Nupeek.Cli/Nupeek.Cli.csproj`
   - `github.run_number`
3. pack Nupeek global tool package with computed version
4. publish package to NuGet.org
5. create and push git tag `v<computed-version>`
6. create GitHub release and attach `.nupkg`

## Version format

`<VersionPrefix>.<github.run_number>`

Example:

- `VersionPrefix=0.1`
- workflow run number `127`
- published version: `0.1.127`
- tag: `v0.1.127`

## Verify published package

```bash
dotnet tool update -g Nupeek
nupeek --help
```

## Manual fallback

If automatic release is temporarily disabled, run the same pack/push steps manually and publish a matching GitHub release tag.
