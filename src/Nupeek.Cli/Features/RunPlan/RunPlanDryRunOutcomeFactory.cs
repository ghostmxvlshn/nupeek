namespace Nupeek.Cli;

internal static class RunPlanDryRunOutcomeFactory
{
    public static CliOutcome Create(PlanRequest request)
        => new(
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
