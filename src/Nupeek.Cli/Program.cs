using Nupeek.Cli;
using Nupeek.Core;
using System.CommandLine;

return await BuildRootCommand().InvokeAsync(args).ConfigureAwait(false);

static RootCommand BuildRootCommand()
{
    var verboseOption = new Option<bool>("--verbose", "Show extra diagnostics on stderr.");
    var quietOption = new Option<bool>("--quiet", "Suppress non-essential stdout output.");
    var dryRunOption = new Option<bool>("--dry-run", () => true, "Show execution plan without decompiling.");

    var root = new RootCommand("Nupeek: targeted NuGet decompilation for coding agents.")
    {
        TreatUnmatchedTokensAsErrors = true
    };

    root.AddGlobalOption(verboseOption);
    root.AddGlobalOption(quietOption);
    root.AddGlobalOption(dryRunOption);

    root.Description += Environment.NewLine + Environment.NewLine +
        "Examples:" + Environment.NewLine +
        "  nupeek type --package Azure.Messaging.ServiceBus --type Azure.Messaging.ServiceBus.ServiceBusSender --out deps-src" + Environment.NewLine +
        "  nupeek find --package Polly --symbol Polly.Policy.Handle --out deps-src";

    root.AddCommand(BuildTypeCommand(verboseOption, quietOption, dryRunOption));
    root.AddCommand(BuildFindCommand(verboseOption, quietOption, dryRunOption));

    return root;
}

static Command BuildTypeCommand(Option<bool> verboseOption, Option<bool> quietOption, Option<bool> dryRunOption)
{
    var packageOption = new Option<string>("--package", "NuGet package id") { IsRequired = true };
    packageOption.AddAlias("-p");

    var versionOption = new Option<string?>("--version", "NuGet package version. Defaults to latest.");
    var tfmOption = new Option<string?>("--tfm", "Target framework moniker. Defaults to auto.");

    var typeOption = new Option<string>("--type", "Fully-qualified type name (e.g. Namespace.Type)") { IsRequired = true };
    var outOption = new Option<string>("--out", "Output directory (e.g. deps-src)") { IsRequired = true };

    var command = new Command("type", "Decompile a single type from a NuGet package.");
    command.AddOption(packageOption);
    command.AddOption(versionOption);
    command.AddOption(tfmOption);
    command.AddOption(typeOption);
    command.AddOption(outOption);

    command.SetHandler((string package, string? version, string? tfm, string type, string @out, bool verbose, bool quiet, bool dryRun) =>
        {
            var exitCode = RunPlan("type", package, version ?? "latest", tfm ?? "auto", type, @out, verbose, quiet, dryRun);
            Environment.ExitCode = exitCode;
        },
        packageOption, versionOption, tfmOption, typeOption, outOption, verboseOption, quietOption, dryRunOption);

    return command;
}

static Command BuildFindCommand(Option<bool> verboseOption, Option<bool> quietOption, Option<bool> dryRunOption)
{
    var packageOption = new Option<string>("--package", "NuGet package id") { IsRequired = true };
    packageOption.AddAlias("-p");

    var versionOption = new Option<string?>("--version", "NuGet package version. Defaults to latest.");
    var tfmOption = new Option<string?>("--tfm", "Target framework moniker. Defaults to auto.");

    var symbolOption = new Option<string>("--symbol", "Symbol name, e.g. Namespace.Type.Method") { IsRequired = true };
    var outOption = new Option<string>("--out", "Output directory (e.g. deps-src)") { IsRequired = true };

    var command = new Command("find", "Resolve symbol to type and decompile that type.");
    command.AddOption(packageOption);
    command.AddOption(versionOption);
    command.AddOption(tfmOption);
    command.AddOption(symbolOption);
    command.AddOption(outOption);

    command.SetHandler((string package, string? version, string? tfm, string symbol, string @out, bool verbose, bool quiet, bool dryRun) =>
        {
            try
            {
                var typeName = SymbolParser.ToTypeName(symbol);
                var exitCode = RunPlan("find", package, version ?? "latest", tfm ?? "auto", typeName, @out, verbose, quiet, dryRun, symbol);
                Environment.ExitCode = exitCode;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"Invalid symbol: {ex.Message}");
                Console.Error.WriteLine("Try: --symbol Namespace.Type.Method");
                Environment.ExitCode = ExitCodes.InvalidArguments;
            }
        },
        packageOption, versionOption, tfmOption, symbolOption, outOption, verboseOption, quietOption, dryRunOption);

    return command;
}

static int RunPlan(
    string command,
    string package,
    string version,
    string tfm,
    string type,
    string outDir,
    bool verbose,
    bool quiet,
    bool dryRun,
    string? sourceSymbol = null)
{
    if (verbose)
    {
        Console.Error.WriteLine("[nupeek] preparing execution plan...");
    }

    if (!quiet)
    {
        Console.WriteLine("Nupeek execution plan");
        Console.WriteLine($"command: {command}");
        Console.WriteLine($"package: {package}");
        Console.WriteLine($"version: {version}");
        Console.WriteLine($"tfm: {tfm}");
        if (!string.IsNullOrWhiteSpace(sourceSymbol))
        {
            Console.WriteLine($"symbol: {sourceSymbol}");
        }
        Console.WriteLine($"type: {type}");
        Console.WriteLine($"out: {outDir}");
        Console.WriteLine($"dryRun: {dryRun}");
    }

    if (verbose)
    {
        Console.Error.WriteLine("[nupeek] completed.");
    }

    return ExitCodes.Success;
}
