using System.CommandLine;

namespace Nupeek.Cli;

internal sealed record GlobalCliOptions(
    Option<bool> Verbose,
    Option<bool> Quiet,
    Option<bool> DryRun,
    Option<string> Progress);
