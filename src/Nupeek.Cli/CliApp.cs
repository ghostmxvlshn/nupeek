using Nupeek.Core;
using System.CommandLine;
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
        catch (Exception ex) when (ex.InnerException is ArgumentException innerArg)
        {
            Console.Error.WriteLine(innerArg.Message);
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
        var progressOption = new Option<string>("--progress", () => "auto", "Progress indicator: auto (default), always, never.");

        var root = new RootCommand("Nupeek: targeted NuGet decompilation for coding agents.")
        {
            TreatUnmatchedTokensAsErrors = true,
        };

        root.AddGlobalOption(verboseOption);
        root.AddGlobalOption(quietOption);
        root.AddGlobalOption(dryRunOption);
        root.AddGlobalOption(progressOption);

        var globalOptions = new GlobalCliOptions(verboseOption, quietOption, dryRunOption, progressOption);

        root.Description += Environment.NewLine + Environment.NewLine +
            "Examples:" + Environment.NewLine +
            "  nupeek type --package Azure.Messaging.ServiceBus --type Azure.Messaging.ServiceBus.ServiceBusSender --out deps-src" + Environment.NewLine +
            "  nupeek find --package Polly --symbol Polly.Policy.Handle --out deps-src";

        root.AddCommand(TypeCommandFactory.Create(globalOptions, RunPlanAsync));
        root.AddCommand(FindCommandFactory.Create(globalOptions, RunPlanAsync));

        return root;
    }

    private static async Task<int> RunPlanAsync(PlanRequest request)
    {
        var format = InputValidation.NormalizeFormat(request.Format);
        var emit = InputValidation.NormalizeEmit(request.Emit);
        var progress = InputValidation.NormalizeProgress(request.Progress);
        var maxChars = InputValidation.NormalizeMaxChars(request.MaxChars);

        if (request.Verbose)
        {
            Console.Error.WriteLine("[nupeek] preparing execution plan...");
        }

        var outcome = await ExecutePlanAsync(request, format, emit, progress, maxChars).ConfigureAwait(false);
        EmitOutcome(outcome, request, format, emit);

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
            null,
            null,
            string.Equals(request.Emit, "agent", StringComparison.OrdinalIgnoreCase) ? request.MaxChars : null,
            null,
            false);
    }

    private static Task<CliOutcome> ExecutePlanAsync(PlanRequest request, string format, string emit, string progress, int maxChars)
    {
        if (request.DryRun)
        {
            return Task.FromResult(HandleDryRun(request));
        }

        if (!ShouldShowSpinner(request, format, progress))
        {
            return HandleRealRunAsync(request, emit, maxChars);
        }

        return ExecuteWithSpinnerAsync(() => HandleRealRunAsync(request, emit, maxChars));
    }

    private static bool ShouldShowSpinner(PlanRequest request, string format, string progress)
    {
        if (request.Quiet || request.Verbose || string.Equals(format, "json", StringComparison.Ordinal))
        {
            return false;
        }

        if (string.Equals(progress, "always", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(progress, "never", StringComparison.Ordinal))
        {
            return false;
        }

        return !Console.IsErrorRedirected;
    }

    private static async Task<CliOutcome> ExecuteWithSpinnerAsync(Func<Task<CliOutcome>> action)
    {
        using var spinner = new Spinner("Executing Nupeek", Console.Error);
        spinner.Start();

        CliOutcome? outcome = null;

        try
        {
            outcome = await action().ConfigureAwait(false);
            return outcome;
        }
        finally
        {
            var status = outcome is not null && outcome.ExitCode == ExitCodes.Success ? "Done" : "Failed";
            spinner.Stop(status);
        }
    }

    private static async Task<CliOutcome> HandleRealRunAsync(PlanRequest request, string emit, int maxChars)
    {
        try
        {
            var pipeline = new TypeDecompilePipeline();
            var result = await pipeline.RunAsync(new TypeDecompileRequest(
                request.Package,
                string.Equals(request.Version, "latest", StringComparison.OrdinalIgnoreCase) ? null : request.Version,
                string.Equals(request.Tfm, "auto", StringComparison.OrdinalIgnoreCase) ? null : request.Tfm,
                request.Type,
                request.OutDir)).ConfigureAwait(false);

            var inlineSource = InlineSourceReader.ReadInlineSource(result.OutputPath, emit, maxChars);

            return new CliOutcome(
                ExitCodes.Success,
                null,
                result.PackageId,
                result.Version,
                result.Tfm,
                result.AssemblyPath,
                result.OutputPath,
                result.IndexPath,
                result.ManifestPath,
                inlineSource.Content,
                inlineSource.MaxChars,
                inlineSource.OriginalChars,
                inlineSource.Truncated);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return new CliOutcome(ExitCodes.TypeOrSymbolNotFound, ex.Message, request.Package, request.Version, request.Tfm, null, null, null, null, null, null, null, false);
        }
        catch (InvalidOperationException ex)
        {
            return new CliOutcome(ExitCodes.PackageResolutionFailure, ex.Message, request.Package, request.Version, request.Tfm, null, null, null, null, null, null, null, false);
        }
        catch (Exception ex)
        {
            return new CliOutcome(ExitCodes.DecompilationFailure, $"Decompilation failed: {ex.Message}", request.Package, request.Version, request.Tfm, null, null, null, null, null, null, null, false);
        }
    }

    private static void EmitOutcome(CliOutcome outcome, PlanRequest request, string format, string emit)
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
                emit,
                outcome.InlineSource,
                outcome.MaxChars,
                outcome.OriginalChars,
                outcome.Truncated,
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

            if (string.Equals(emit, "agent", StringComparison.Ordinal) && !string.IsNullOrEmpty(outcome.InlineSource))
            {
                Console.WriteLine("--- inlineSource:start ---");
                Console.WriteLine(outcome.InlineSource);
                Console.WriteLine("--- inlineSource:end ---");
                if (outcome.Truncated)
                {
                    Console.WriteLine($"inlineSourceTruncated: true ({outcome.MaxChars}/{outcome.OriginalChars} chars)");
                }
            }
        }
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
}
