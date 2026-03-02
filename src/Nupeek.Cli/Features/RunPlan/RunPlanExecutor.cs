namespace Nupeek.Cli;

internal static class RunPlanExecutor
{
    public static Task<CliOutcome> ExecuteAsync(
        PlanRequest request,
        string progress,
        CancellationToken cancellationToken)
    {
        if (request.DryRun)
        {
            return Task.FromResult(RunPlanDryRunOutcomeFactory.Create(request));
        }

        if (!RunPlanSpinnerPolicy.ShouldShow(request, progress))
        {
            return RunPlanRealExecution.ExecuteAsync(request, cancellationToken);
        }

        return RunPlanSpinnerExecutor.ExecuteAsync(
            () => RunPlanRealExecution.ExecuteAsync(request, cancellationToken));
    }
}
