namespace Nupeek.Cli;

internal static class RunPlanHandler
{
    public static async Task<int> RunAsync(PlanRequest request, CancellationToken cancellationToken)
    {
        var progress = InputValidation.NormalizeProgress(request.Progress);

        if (request.Verbose)
        {
            Console.Error.WriteLine("[nupeek] preparing execution plan...");
        }

        var outcome = await RunPlanExecutor.ExecuteAsync(request, progress, cancellationToken).ConfigureAwait(false);
        RunPlanOutcomeEmitter.Emit(outcome, request);

        if (request.Verbose)
        {
            Console.Error.WriteLine("[nupeek] completed.");
        }

        return outcome.ExitCode;
    }
}
