# Static Code Analysis in Nupeek

Nupeek uses well-known analyzer packages to enforce C# best practices:

- `Microsoft.CodeAnalysis.NetAnalyzers`
  - Official .NET analyzers (reliability, performance, maintainability, security-adjacent checks)
- `Meziantou.Analyzer`
  - Community analyzer set with pragmatic code-quality and API-usage checks

Configuration lives in `Directory.Build.props` and `.editorconfig`.

## Principles
- Keep warnings meaningful and actionable.
- Prefer recommended rules over noisy exhaustive defaults.
- Tighten severity over time as the codebase stabilizes.

## Local workflow
Before committing, use:

```bash
dotnet format Nupeek.slnx --verify-no-changes
dotnet test Nupeek.slnx --configuration Release
```
