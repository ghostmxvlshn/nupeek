using System.CommandLine;

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
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        Console.CancelKeyPress += cancelHandler;

        try
        {
            var root = BuildRootCommand(cancellation.Token);

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
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Operation canceled.");
            return ExitCodes.OperationCanceled;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            return ExitCodes.GenericError;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    /// <summary>
    /// Creates the root command and global options.
    /// </summary>
    public static RootCommand BuildRootCommand(CancellationToken cancellationToken)
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
            "Command options:" + Environment.NewLine +
            "  type: --package|-p --type --out [--version] [--tfm] [--format text|json] [--emit files|agent] [--max-chars N]" + Environment.NewLine +
            "  find: --package|-p --symbol --out [--version] [--tfm] [--format text|json] [--emit files|agent] [--max-chars N]" + Environment.NewLine + Environment.NewLine +
            "Tip:" + Environment.NewLine +
            "  Run 'nupeek <command> --help' to see full per-command options." + Environment.NewLine + Environment.NewLine +
            "Examples:" + Environment.NewLine +
            "  nupeek type --package Azure.Messaging.ServiceBus --type Azure.Messaging.ServiceBus.ServiceBusSender --out deps-src" + Environment.NewLine +
            "  nupeek type --package Humanizer.Core --version 2.14.1 --tfm netstandard2.0 --type Humanizer.StringHumanizeExtensions --out deps-src --dry-run false" + Environment.NewLine +
            "  nupeek type --package Polly --type Polly.Policy --out deps-src --format json --emit agent --max-chars 4000 --dry-run false" + Environment.NewLine +
            "  nupeek find --package Polly --symbol Polly.Policy.Handle --out deps-src" + Environment.NewLine +
            "  nupeek find --package Dapper --symbol Dapper.SqlMapper.Query --out deps-src --progress never --dry-run false";

        root.AddCommand(TypeCommandFactory.Create(globalOptions, request => RunPlanHandler.RunAsync(request, cancellationToken)));
        root.AddCommand(FindCommandFactory.Create(globalOptions, request => RunPlanHandler.RunAsync(request, cancellationToken)));

        return root;
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
        => RunPlanTextBuilder.Build(command, package, version, tfm, type, outDir, dryRun, sourceSymbol);
}
