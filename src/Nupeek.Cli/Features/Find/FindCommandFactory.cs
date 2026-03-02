using Nupeek.Core;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Nupeek.Cli;

internal static class FindCommandFactory
{
    public static Command Create(GlobalCliOptions globalOptions, Func<PlanRequest, Task<int>> runPlanAsync)
    {
        var packageOption = new Option<string?>("--package", "NuGet package id.");
        packageOption.AddAlias("-p");

        var assemblyOption = new Option<string?>("--assembly", "Path to local assembly (.dll). Prefer this when dependency is already restored.");
        var versionOption = new Option<string?>("--version", "NuGet package version. Defaults to latest. Ignored with --assembly.");
        var tfmOption = new Option<string?>("--tfm", "Target framework moniker. Defaults to auto.");
        var symbolOption = new Option<string>("--symbol", "Symbol name, e.g. Namespace.Type.Method") { IsRequired = true };
        var outOption = new Option<string>("--out", "Output directory (e.g. deps-src)") { IsRequired = true };
        var formatOption = new Option<string>("--format", () => "text", "Output format: text (default) or json.");
        var emitOption = new Option<string>("--emit", () => "files", "Emit mode: files (default) or agent.");
        var maxCharsOption = new Option<int>("--max-chars", () => 12000, "Max inline source chars for --emit agent.");

        var command = new Command("find", "Resolve symbol to type and decompile that type.");
        command.AddOption(packageOption);
        command.AddOption(assemblyOption);
        command.AddOption(versionOption);
        command.AddOption(tfmOption);
        command.AddOption(symbolOption);
        command.AddOption(outOption);
        command.AddOption(formatOption);
        command.AddOption(emitOption);
        command.AddOption(maxCharsOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var parse = context.ParseResult;
            var package = parse.GetValueForOption(packageOption);
            var assembly = parse.GetValueForOption(assemblyOption);

            var validation = ValidateSource(package, assembly);
            if (validation is not null)
            {
                Console.Error.WriteLine(validation);
                Environment.ExitCode = ExitCodes.InvalidArguments;
                return;
            }

            var symbol = parse.GetValueForOption(symbolOption)!;
            Environment.ExitCode = await runPlanAsync(new PlanRequest(
                Command: "find",
                Package: package,
                Assembly: assembly,
                Version: parse.GetValueForOption(versionOption) ?? "latest",
                Tfm: parse.GetValueForOption(tfmOption) ?? "auto",
                Type: SymbolParser.ToTypeName(symbol),
                OutDir: parse.GetValueForOption(outOption)!,
                Verbose: parse.GetValueForOption(globalOptions.Verbose),
                Quiet: parse.GetValueForOption(globalOptions.Quiet),
                DryRun: parse.GetValueForOption(globalOptions.DryRun),
                Format: parse.GetValueForOption(formatOption) ?? "text",
                Emit: parse.GetValueForOption(emitOption) ?? "files",
                MaxChars: parse.GetValueForOption(maxCharsOption),
                Progress: parse.GetValueForOption(globalOptions.Progress) ?? "auto",
                SourceSymbol: symbol)).ConfigureAwait(false);
        });

        return command;
    }

    private static string? ValidateSource(string? package, string? assembly)
    {
        if (string.IsNullOrWhiteSpace(package) && string.IsNullOrWhiteSpace(assembly))
        {
            return "Provide exactly one source: --package <id> or --assembly <path-to-dll>.";
        }

        if (!string.IsNullOrWhiteSpace(package) && !string.IsNullOrWhiteSpace(assembly))
        {
            return "Use either --package or --assembly, not both.";
        }

        return null;
    }
}
