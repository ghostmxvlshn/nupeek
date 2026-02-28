namespace Nupeek.Cli;

internal sealed record PlanRequest(
    string Command,
    string Package,
    string Version,
    string Tfm,
    string Type,
    string OutDir,
    bool Verbose,
    bool Quiet,
    bool DryRun,
    string Format,
    string Emit,
    int MaxChars,
    string Progress,
    string? SourceSymbol);
