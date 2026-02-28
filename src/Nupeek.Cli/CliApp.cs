using Nupeek.Core;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using System.Text.Json;

namespace Nupeek.Cli;

/// <summary>
/// Builds and executes Nupeek CLI command tree.
/// </summary>
public static class CliApp
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Runs CLI flow and maps errors to stable exit codes.
    /// </summary>
    public static async Task<int> RunAsync(string[] args, IConsole? console = null)
    {
        var root = BuildRootCommand();

        if (args.Any(static a => a is "--help" or "-h"))
        {
            return await root.InvokeAsync(args, console).ConfigureAwait(false);
        }

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

    private static Command BuildTypeCommand(Option<bool> verboseOption, Option<bool> quietOption, Option<bool> dryRunOption)
    {
        var packageOption = new Option<string>("--package", "NuGet package id") { IsRequired = true };
        packageOption.AddAlias("-p");

        var versionOption = new Option<string?>("--version", "NuGet package version. Defaults to latest.");
        var tfmOption = new Option<string?>("--tfm", "Target framework moniker. Defaults to auto.");
        var typeOption = new Option<string>("--type", "Fully-qualified type name (e.g. Namespace.Type)") { IsRequired = true };
        var outOption = new Option<string>("--out", "Output directory (e.g. deps-src)") { IsRequired = true };
        var formatOption = new Option<string>("--format", () => "text", "Output format: text (default) or json.");

        var command = new Command("type", "Decompile a single type from a NuGet package.");
        command.AddOption(packageOption);
        command.AddOption(versionOption);
        command.AddOption(tfmOption);
        command.AddOption(typeOption);
        command.AddOption(outOption);
        command.AddOption(formatOption);

        command.SetHandler((InvocationContext context) =>
        {
            var parse = context.ParseResult;
            Environment.ExitCode = RunPlan(new PlanRequest(
                Command: "type",
                Package: parse.GetValueForOption(packageOption)!,
                Version: parse.GetValueForOption(versionOption) ?? "latest",
                Tfm: parse.GetValueForOption(tfmOption) ?? "auto",
                Type: parse.GetValueForOption(typeOption)!,
                OutDir: parse.GetValueForOption(outOption)!,
                Verbose: parse.GetValueForOption(verboseOption),
                Quiet: parse.GetValueForOption(quietOption),
                DryRun: parse.GetValueForOption(dryRunOption),
                Format: parse.GetValueForOption(formatOption) ?? "text",
                SourceSymbol: null));
        });

        return command;
    }

    private static Command BuildFindCommand(Option<bool> verboseOption, Option<bool> quietOption, Option<bool> dryRunOption)
    {
        var packageOption = new Option<string>("--package", "NuGet package id") { IsRequired = true };
        packageOption.AddAlias("-p");

        var versionOption = new Option<string?>("--version", "NuGet package version. Defaults to latest.");
        var tfmOption = new Option<string?>("--tfm", "Target framework moniker. Defaults to auto.");
        var symbolOption = new Option<string>("--symbol", "Symbol name, e.g. Namespace.Type.Method") { IsRequired = true };
        var outOption = new Option<string>("--out", "Output directory (e.g. deps-src)") { IsRequired = true };
        var formatOption = new Option<string>("--format", () => "text", "Output format: text (default) or json.");

        var command = new Command("find", "Resolve symbol to type and decompile that type.");
        command.AddOption(packageOption);
        command.AddOption(versionOption);
        command.AddOption(tfmOption);
        command.AddOption(symbolOption);
        command.AddOption(outOption);
        command.AddOption(formatOption);

        command.SetHandler((InvocationContext context) =>
        {
            var parse = context.ParseResult;
            var symbol = parse.GetValueForOption(symbolOption)!;
            Environment.ExitCode = RunPlan(new PlanRequest(
                Command: "find",
                Package: parse.GetValueForOption(packageOption)!,
                Version: parse.GetValueForOption(versionOption) ?? "latest",
                Tfm: parse.GetValueForOption(tfmOption) ?? "auto",
                Type: SymbolParser.ToTypeName(symbol),
                OutDir: parse.GetValueForOption(outOption)!,
                Verbose: parse.GetValueForOption(verboseOption),
                Quiet: parse.GetValueForOption(quietOption),
                DryRun: parse.GetValueForOption(dryRunOption),
                Format: parse.GetValueForOption(formatOption) ?? "text",
                SourceSymbol: symbol));
        });

        return command;
    }

    private static int RunPlan(PlanRequest request)
    {
        var format = NormalizeFormat(request.Format);

        if (request.Verbose)
        {
            Console.Error.WriteLine("[nupeek] preparing execution plan...");
        }

        var outcome = request.DryRun ? HandleDryRun(request) : HandleRealRun(request);
        EmitOutcome(outcome, request, format);

        if (request.Verbose)
        {
            Console.Error.WriteLine("[nupeek] completed.");
        }

        return outcome.ExitCode;
    }

    private static CliOutcome HandleDryRun(PlanRequest request)
    {
        return new CliOutcome(
            ExitCodes.Success,
            null,
            request.Package,
            request.Version,
            request.Tfm,
            null,
            null,
            null,
            null);
    }

    private static CliOutcome HandleRealRun(PlanRequest request)
    {
        try
        {
            var pipeline = new TypeDecompilePipeline();
            var result = pipeline.RunAsync(new TypeDecompileRequest(
                request.Package,
                string.Equals(request.Version, "latest", StringComparison.OrdinalIgnoreCase) ? null : request.Version,
                string.Equals(request.Tfm, "auto", StringComparison.OrdinalIgnoreCase) ? null : request.Tfm,
                request.Type,
                request.OutDir)).GetAwaiter().GetResult();

            return new CliOutcome(
                ExitCodes.Success,
                null,
                result.PackageId,
                result.Version,
                result.Tfm,
                result.AssemblyPath,
                result.OutputPath,
                result.IndexPath,
                result.ManifestPath);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return new CliOutcome(ExitCodes.TypeOrSymbolNotFound, ex.Message, request.Package, request.Version, request.Tfm, null, null, null, null);
        }
        catch (InvalidOperationException ex)
        {
            return new CliOutcome(ExitCodes.PackageResolutionFailure, ex.Message, request.Package, request.Version, request.Tfm, null, null, null, null);
        }
        catch (Exception ex)
        {
            return new CliOutcome(ExitCodes.DecompilationFailure, $"Decompilation failed: {ex.Message}", request.Package, request.Version, request.Tfm, null, null, null, null);
        }
    }

    private static void EmitOutcome(CliOutcome outcome, PlanRequest request, string format)
    {
        if (string.Equals(format, "json", StringComparison.Ordinal))
        {
            WriteJson(new CliRunResult(
                request.Command,
                outcome.PackageId,
                outcome.Version,
                outcome.SelectedTfm,
                string.Equals(request.Command, "type", StringComparison.Ordinal) ? request.Type : null,
                request.SourceSymbol,
                request.Type,
                outcome.AssemblyPath,
                outcome.OutputPath,
                outcome.IndexPath,
                outcome.ManifestPath,
                request.DryRun,
                outcome.ExitCode,
                outcome.Error));
            return;
        }

        if (outcome.ExitCode != ExitCodes.Success)
        {
            Console.Error.WriteLine(outcome.Error);
            return;
        }

        if (request.Quiet)
        {
            return;
        }

        Console.WriteLine(BuildPlanText(request.Command, request.Package, request.Version, request.Tfm, request.Type, request.OutDir, request.DryRun, request.SourceSymbol));

        if (!request.DryRun)
        {
            Console.WriteLine($"outputPath: {outcome.OutputPath}");
            Console.WriteLine($"indexPath: {outcome.IndexPath}");
            Console.WriteLine($"manifestPath: {outcome.ManifestPath}");
        }
    }

    private static string NormalizeFormat(string format)
    {
        if (string.Equals(format, "text", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            return format.ToLowerInvariant();
        }

        throw new ArgumentException("Invalid --format value. Allowed: text, json.", nameof(format));
    }

    private static void WriteJson(CliRunResult payload)
    {
        Console.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
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

    private sealed record PlanRequest(
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
        string? SourceSymbol);

    private sealed record CliOutcome(
        int ExitCode,
        string? Error,
        string PackageId,
        string Version,
        string SelectedTfm,
        string? AssemblyPath,
        string? OutputPath,
        string? IndexPath,
        string? ManifestPath);

    private sealed record CliRunResult(
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
        bool DryRun,
        int ExitCode,
        string? Error);
}
