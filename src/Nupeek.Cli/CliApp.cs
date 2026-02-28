using Nupeek.Core;
using System.CommandLine;
using System.Text;

namespace Nupeek.Cli;

/// <summary>
/// Builds and executes Nupeek CLI command tree.
/// </summary>
public static class CliApp
{
    /// <summary>
    /// Runs CLI flow and maps errors to stable exit codes.
    /// </summary>
    public static async Task<int> RunAsync(string[] args, IConsole? console = null)
    {
        var root = BuildRootCommand();

        // Let command-line library render help path as-is.
        if (args.Any(static a => a is "--help" or "-h"))
        {
            var helpCode = await root.InvokeAsync(args, console).ConfigureAwait(false);
            return helpCode;
        }

        // Parse first to provide concise argument diagnostics.
        var parseResult = root.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
            {
                Console.Error.WriteLine(error.Message);
            }

            Console.Error.WriteLine("Run 'nupeek --help' for usage.");
            return ExitCodes.InvalidArguments;
        }

        try
        {
            // System.CommandLine beta handlers here set Environment.ExitCode; InvokeAsync may still return 0.
            Environment.ExitCode = ExitCodes.Success;
            var invokeCode = await root.InvokeAsync(args, console).ConfigureAwait(false);
            return invokeCode != ExitCodes.Success ? invokeCode : Environment.ExitCode;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return ExitCodes.InvalidArguments;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            return ExitCodes.GenericError;
        }
    }

    /// <summary>
    /// Creates the root command and global options.
    /// </summary>
    public static RootCommand BuildRootCommand()
    {
        var verboseOption = new Option<bool>("--verbose", "Show extra diagnostics on stderr.");
        var quietOption = new Option<bool>("--quiet", "Suppress non-essential stdout output.");
        var dryRunOption = new Option<bool>("--dry-run", () => true, "Show execution plan without decompiling.");

        var root = new RootCommand("Nupeek: targeted NuGet decompilation for coding agents.")
        {
            TreatUnmatchedTokensAsErrors = true,
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

    /// <summary>
    /// Builds <c>type</c> command and handler.
    /// </summary>
    private static Command BuildTypeCommand(Option<bool> verboseOption, Option<bool> quietOption, Option<bool> dryRunOption)
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

        command.SetHandler(
            (string package, string? version, string? tfm, string type, string @out, bool verbose, bool quiet, bool dryRun) =>
            {
                Environment.ExitCode = RunPlan("type", package, version ?? "latest", tfm ?? "auto", type, @out, verbose, quiet, dryRun);
            },
            packageOption,
            versionOption,
            tfmOption,
            typeOption,
            outOption,
            verboseOption,
            quietOption,
            dryRunOption);

        return command;
    }

    /// <summary>
    /// Builds <c>find</c> command and handler.
    /// </summary>
    private static Command BuildFindCommand(Option<bool> verboseOption, Option<bool> quietOption, Option<bool> dryRunOption)
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

        command.SetHandler(
            (string package, string? version, string? tfm, string symbol, string @out, bool verbose, bool quiet, bool dryRun) =>
            {
                // Convert symbol-like input to type name before executing pipeline.
                var typeName = SymbolParser.ToTypeName(symbol);
                Environment.ExitCode = RunPlan("find", package, version ?? "latest", tfm ?? "auto", typeName, @out, verbose, quiet, dryRun, symbol);
            },
            packageOption,
            versionOption,
            tfmOption,
            symbolOption,
            outOption,
            verboseOption,
            quietOption,
            dryRunOption);

        return command;
    }

    /// <summary>
    /// Executes dry-run plan output or real decompilation pipeline.
    /// </summary>
    private static int RunPlan(
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
            Console.WriteLine(BuildPlanText(command, package, version, tfm, type, outDir, dryRun, sourceSymbol));
        }

        if (!dryRun)
        {
            try
            {
                // Real execution: package -> locate type -> decompile -> write catalogs.
                var pipeline = new TypeDecompilePipeline();
                var result = pipeline.RunAsync(new TypeDecompileRequest(
                    package,
                    string.Equals(version, "latest", StringComparison.OrdinalIgnoreCase) ? null : version,
                    string.Equals(tfm, "auto", StringComparison.OrdinalIgnoreCase) ? null : tfm,
                    type,
                    outDir)).GetAwaiter().GetResult();

                if (!quiet)
                {
                    Console.WriteLine($"outputPath: {result.OutputPath}");
                    Console.WriteLine($"indexPath: {result.IndexPath}");
                    Console.WriteLine($"manifestPath: {result.ManifestPath}");
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine(ex.Message);
                return ExitCodes.TypeOrSymbolNotFound;
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return ExitCodes.PackageResolutionFailure;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Decompilation failed: {ex.Message}");
                return ExitCodes.DecompilationFailure;
            }
        }

        if (verbose)
        {
            Console.Error.WriteLine("[nupeek] completed.");
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// Builds plain-text execution plan output for user visibility.
    /// </summary>
    public static string BuildPlanText(
        string command,
        string package,
        string version,
        string tfm,
        string type,
        string outDir,
        bool dryRun,
        string? sourceSymbol = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Nupeek execution plan");
        sb.AppendLine($"command: {command}");
        sb.AppendLine($"package: {package}");
        sb.AppendLine($"version: {version}");
        sb.AppendLine($"tfm: {tfm}");

        if (!string.IsNullOrWhiteSpace(sourceSymbol))
        {
            sb.AppendLine($"symbol: {sourceSymbol}");
        }

        sb.AppendLine($"type: {type}");
        sb.AppendLine($"out: {outDir}");
        sb.AppendLine($"dryRun: {dryRun}");

        return sb.ToString().TrimEnd();
    }
}
