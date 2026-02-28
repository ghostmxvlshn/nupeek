namespace Nupeek.Cli;

internal sealed record CliRunResult(
    string Command,
    string PackageId,
    string Version,
    string SelectedTfm,
    string? InputType,
    string? InputSymbol,
    string ResolvedType,
    string? AssemblyPath,
    string? OutputPath,
    string? IndexPath,
    string? ManifestPath,
    string Emit,
    string? InlineSource,
    int? MaxChars,
    int? OriginalChars,
    bool Truncated,
    bool DryRun,
    int ExitCode,
    string? Error);
