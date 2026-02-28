namespace Nupeek.Cli;

internal static class RunPlanHandler
{
    public static async Task<int> RunAsync(PlanRequest request, CancellationToken cancellationToken)
    {
        var format = InputValidation.NormalizeFormat(request.Format);
        var emit = InputValidation.NormalizeEmit(request.Emit);
        var progress = InputValidation.NormalizeProgress(request.Progress);
        var maxChars = InputValidation.NormalizeMaxChars(request.MaxChars);

        if (request.Verbose)
        {
            Console.Error.WriteLine("[nupeek] preparing execution plan...");
        }

        var outcome = await RunPlanExecutor.ExecuteAsync(request, format, emit, progress, maxChars, cancellationToken).ConfigureAwait(false);
        RunPlanOutcomeEmitter.Emit(outcome, request, format, emit);

        if (request.Verbose)
        {
            Console.Error.WriteLine("[nupeek] completed.");
        }

        return outcome.ExitCode;
    }
}
