using Nupeek.Core;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
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

        root.Description += Environment.NewLine + Environment.NewLine +
            "Examples:" + Environment.NewLine +
            "  nupeek type --package Azure.Messaging.ServiceBus --type Azure.Messaging.ServiceBus.ServiceBusSender --out deps-src" + Environment.NewLine +
            "  nupeek find --package Polly --symbol Polly.Policy.Handle --out deps-src";

        root.AddCommand(BuildTypeCommand(verboseOption, quietOption, dryRunOption, progressOption));
        root.AddCommand(BuildFindCommand(verboseOption, quietOption, dryRunOption, progressOption));

        return root;
    }

    private static Command BuildTypeCommand(Option<bool> verboseOption, Option<bool> quietOption, Option<bool> dryRunOption, Option<string> progressOption)
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
                Emit: parse.GetValueForOption(emitOption) ?? "files",
                MaxChars: parse.GetValueForOption(maxCharsOption),
                Progress: parse.GetValueForOption(progressOption) ?? "auto",
                SourceSymbol: null));
        });

        return command;
    }

    private static Command BuildFindCommand(Option<bool> verboseOption, Option<bool> quietOption, Option<bool> dryRunOption, Option<string> progressOption)
    {
        var packageOption = new Option<string>("--package", "NuGet package id") { IsRequired = true };
        packageOption.AddAlias("-p");

        var versionOption = new Option<string?>("--version", "NuGet package version. Defaults to latest.");
        var tfmOption = new Option<string?>("--tfm", "Target framework moniker. Defaults to auto.");
        var symbolOption = new Option<string>("--symbol", "Symbol name, e.g. Namespace.Type.Method") { IsRequired = true };
        var outOption = new Option<string>("--out", "Output directory (e.g. deps-src)") { IsRequired = true };
        var formatOption = new Option<string>("--format", () => "text", "Output format: text (default) or json.");
        var emitOption = new Option<string>("--emit", () => "files", "Emit mode: files (default) or agent.");
        var maxCharsOption = new Option<int>("--max-chars", () => 12000, "Max inline source chars for --emit agent.");

        var command = new Command("find", "Resolve symbol to type and decompile that type.");
        command.AddOption(packageOption);
        command.AddOption(versionOption);
        command.AddOption(tfmOption);
        command.AddOption(symbolOption);
        command.AddOption(outOption);
        command.AddOption(formatOption);
        command.AddOption(emitOption);
        command.AddOption(maxCharsOption);

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
                Emit: parse.GetValueForOption(emitOption) ?? "files",
                MaxChars: parse.GetValueForOption(maxCharsOption),
                Progress: parse.GetValueForOption(progressOption) ?? "auto",
                SourceSymbol: symbol));
        });

        return command;
    }

    private static int RunPlan(PlanRequest request)
    {
        var format = NormalizeFormat(request.Format);
        var emit = NormalizeEmit(request.Emit);
        var progress = NormalizeProgress(request.Progress);
        var maxChars = NormalizeMaxChars(request.MaxChars);

        if (request.Verbose)
        {
            Console.Error.WriteLine("[nupeek] preparing execution plan...");
        }

        var outcome = ExecutePlan(request, format, emit, progress, maxChars);
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

    private static CliOutcome ExecutePlan(PlanRequest request, string format, string emit, string progress, int maxChars)
    {
        if (request.DryRun)
        {
            return HandleDryRun(request);
        }

        if (!ShouldShowSpinner(request, format, progress))
        {
            return HandleRealRun(request, emit, maxChars);
        }

        return ExecuteWithSpinner(request, () => HandleRealRun(request, emit, maxChars));
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

    private static CliOutcome ExecuteWithSpinner(PlanRequest request, Func<CliOutcome> action)
    {
        using var spinner = new Spinner("Executing Nupeek", Console.Error);
        spinner.Start();

        CliOutcome? outcome = null;

        try
        {
            outcome = action();
            return outcome;
        }
        finally
        {
            var status = outcome is not null && outcome.ExitCode == ExitCodes.Success ? "Done" : "Failed";
            spinner.Stop(status);
        }
    }

    private static CliOutcome HandleRealRun(PlanRequest request, string emit, int maxChars)
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

            var inlineSource = ReadInlineSource(result.OutputPath, emit, maxChars);

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

    private static string NormalizeFormat(string format)
    {
        if (string.Equals(format, "text", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            return format.ToLowerInvariant();
        }

        throw new ArgumentException("Invalid --format value. Allowed: text, json.", nameof(format));
    }

    private static string NormalizeProgress(string progress)
    {
        if (string.Equals(progress, "auto", StringComparison.OrdinalIgnoreCase)
            || string.Equals(progress, "always", StringComparison.OrdinalIgnoreCase)
            || string.Equals(progress, "never", StringComparison.OrdinalIgnoreCase))
        {
            return progress.ToLowerInvariant();
        }

        throw new ArgumentException("Invalid --progress value. Allowed: auto, always, never.", nameof(progress));
    }

    private static string NormalizeEmit(string emit)
    {
        if (string.Equals(emit, "files", StringComparison.OrdinalIgnoreCase)
            || string.Equals(emit, "agent", StringComparison.OrdinalIgnoreCase))
        {
            return emit.ToLowerInvariant();
        }

        throw new ArgumentException("Invalid --emit value. Allowed: files, agent.", nameof(emit));
    }

    private static int NormalizeMaxChars(int maxChars)
    {
        if (maxChars < 200)
        {
            throw new ArgumentException("Invalid --max-chars value. Minimum is 200.", nameof(maxChars));
        }

        return maxChars;
    }

    private static InlineSourceResult ReadInlineSource(string outputPath, string emit, int maxChars)
    {
        if (!string.Equals(emit, "agent", StringComparison.Ordinal) || !File.Exists(outputPath))
        {
            return new InlineSourceResult(null, null, null, false);
        }

        var source = File.ReadAllText(outputPath);
        var originalChars = source.Length;
        var truncated = source.Length > maxChars;

        if (truncated)
        {
            source = source[..maxChars];
        }

        return new InlineSourceResult(source, maxChars, originalChars, truncated);
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
        string Emit,
        int MaxChars,
        string Progress,
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
        string? ManifestPath,
        string? InlineSource,
        int? MaxChars,
        int? OriginalChars,
        bool Truncated);

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
        string Emit,
        string? InlineSource,
        int? MaxChars,
        int? OriginalChars,
        bool Truncated,
        bool DryRun,
        int ExitCode,
        string? Error);

    private sealed record InlineSourceResult(
        string? Content,
        int? MaxChars,
        int? OriginalChars,
        bool Truncated);

    private sealed class Spinner : IDisposable
    {
        private static readonly char[] Frames = ['⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏'];

        private readonly TextWriter _writer;
        private readonly string _label;
        private readonly Stopwatch _stopwatch = new();
        private readonly Lock _sync = new();
        private CancellationTokenSource? _cts;
        private Task? _task;
        private int _lastWidth;
        private bool _isRunning;
        private bool _isStopped;

        public Spinner(string label, TextWriter writer)
        {
            _label = label;
            _writer = writer;
        }

        public void Start()
        {
            lock (_sync)
            {
                if (_isRunning)
                {
                    return;
                }

                _cts = new CancellationTokenSource();
                _stopwatch.Restart();
                _isRunning = true;
                _isStopped = false;
                _task = Task.Run(() => RenderLoop(_cts.Token));
            }
        }

        public void Stop(string status)
        {
            StopInternal(status, writeStatus: true);
        }

        public void Dispose()
        {
            StopInternal("", writeStatus: false);

            lock (_sync)
            {
                _cts?.Dispose();
                _cts = null;
                _task = null;
            }
        }

        private void StopInternal(string status, bool writeStatus)
        {
            CancellationTokenSource? cts;
            Task? task;

            lock (_sync)
            {
                if (_isStopped)
                {
                    return;
                }

                _isStopped = true;
                _isRunning = false;
                cts = _cts;
                task = _task;
            }

            cts?.Cancel();

            try
            {
                task?.Wait();
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException or TaskCanceledException))
            {
                // ignore cancellation-only waits
            }

            lock (_sync)
            {
                _stopwatch.Stop();

                if (_lastWidth > 0)
                {
                    _writer.Write("\r" + new string(' ', _lastWidth) + "\r");
                }

                if (writeStatus)
                {
                    _writer.Write($"{status} ({_stopwatch.Elapsed.TotalSeconds:F1}s)\n");
                }

                _writer.Flush();
            }
        }

        private void RenderLoop(CancellationToken token)
        {
            var index = 0;

            while (!token.IsCancellationRequested)
            {
                var text = $"{Frames[index]} {_label}...";

                lock (_sync)
                {
                    _lastWidth = Math.Max(_lastWidth, text.Length);
                    _writer.Write($"\r{text}");
                    _writer.Flush();
                }

                index = (index + 1) % Frames.Length;

                try
                {
                    Task.Delay(80, token).Wait(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
