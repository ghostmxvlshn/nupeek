namespace Nupeek.Cli;

internal sealed record CliOutcome(
    int ExitCode,
    string? Error,
    string PackageId,
    string Version,
    string SelectedTfm,
    string? AssemblyPath,
    string? OutputPath,
    string? IndexPath,
    string? ManifestPath);
