namespace Nupeek.Cli;

internal sealed record PlanRequest(
    string Command,
    string? Package,
    string? Assembly,
    string Version,
    string Tfm,
    string Type,
    int Depth,
    string OutDir,
    bool Verbose,
    bool Quiet,
    bool DryRun,
    string Progress,
    string? SourceSymbol);
