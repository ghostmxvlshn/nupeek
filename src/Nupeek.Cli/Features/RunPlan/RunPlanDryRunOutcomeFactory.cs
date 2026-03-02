namespace Nupeek.Cli;

internal static class RunPlanDryRunOutcomeFactory
{
    public static CliOutcome Create(PlanRequest request)
        => new(
            ExitCodes.Success,
            null,
            RunPlanSourceLabel.Get(request),
            request.Version,
            request.Tfm,
            null,
            null,
            null,
            null);
}
