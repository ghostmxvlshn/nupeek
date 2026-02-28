using System.CommandLine;
using System.CommandLine.Invocation;

namespace Nupeek.Cli;

internal static class TypeCommandFactory
{
    public static Command Create(GlobalCliOptions globalOptions, Func<PlanRequest, Task<int>> runPlanAsync)
    {
        var packageOption = new Option<string>("--package", "NuGet package id") { IsRequired = true };
        packageOption.AddAlias("-p");

        var versionOption = new Option<string?>("--version", "NuGet package version. Defaults to latest.");
        var tfmOption = new Option<string?>("--tfm", "Target framework moniker. Defaults to auto.");
        var typeOption = new Option<string>("--type", "Fully-qualified type name (e.g. Namespace.Type)") { IsRequired = true };
        var outOption = new Option<string>("--out", "Output directory (e.g. deps-src)") { IsRequired = true };
        var formatOption = new Option<string>("--format", () => "text", "Output format: text (default) or json.");
        var emitOption = new Option<string>("--emit", () => "files", "Emit mode: files (default) or agent.");
        var maxCharsOption = new Option<int>("--max-chars", () => 12000, "Max inline source chars for --emit agent.");

        var command = new Command("type", "Decompile a single type from a NuGet package.");
        command.AddOption(packageOption);
        command.AddOption(versionOption);
        command.AddOption(tfmOption);
        command.AddOption(typeOption);
        command.AddOption(outOption);
        command.AddOption(formatOption);
        command.AddOption(emitOption);
        command.AddOption(maxCharsOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var parse = context.ParseResult;
            Environment.ExitCode = await runPlanAsync(new PlanRequest(
                Command: "type",
                Package: parse.GetValueForOption(packageOption)!,
                Version: parse.GetValueForOption(versionOption) ?? "latest",
                Tfm: parse.GetValueForOption(tfmOption) ?? "auto",
                Type: parse.GetValueForOption(typeOption)!,
                OutDir: parse.GetValueForOption(outOption)!,
                Verbose: parse.GetValueForOption(globalOptions.Verbose),
                Quiet: parse.GetValueForOption(globalOptions.Quiet),
                DryRun: parse.GetValueForOption(globalOptions.DryRun),
                Format: parse.GetValueForOption(formatOption) ?? "text",
                Emit: parse.GetValueForOption(emitOption) ?? "files",
                MaxChars: parse.GetValueForOption(maxCharsOption),
                Progress: parse.GetValueForOption(globalOptions.Progress) ?? "auto",
                SourceSymbol: null)).ConfigureAwait(false);
        });

        return command;
    }
}
