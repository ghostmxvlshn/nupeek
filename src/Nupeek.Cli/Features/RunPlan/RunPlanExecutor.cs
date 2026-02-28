namespace Nupeek.Cli;

internal static class RunPlanExecutor
{
    public static Task<CliOutcome> ExecuteAsync(
        PlanRequest request,
        string format,
        string emit,
        string progress,
        int maxChars,
        CancellationToken cancellationToken)
    {
        if (request.DryRun)
        {
            return Task.FromResult(RunPlanDryRunOutcomeFactory.Create(request));
        }

        if (!RunPlanSpinnerPolicy.ShouldShow(request, format, progress))
        {
            return RunPlanRealExecution.ExecuteAsync(request, emit, maxChars, cancellationToken);
        }

        return RunPlanSpinnerExecutor.ExecuteAsync(
            () => RunPlanRealExecution.ExecuteAsync(request, emit, maxChars, cancellationToken));
    }
}
