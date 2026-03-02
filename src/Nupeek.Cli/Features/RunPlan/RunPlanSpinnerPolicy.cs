namespace Nupeek.Cli;

internal static class RunPlanSpinnerPolicy
{
    public static bool ShouldShow(PlanRequest request, string progress)
    {
        if (request.Quiet || request.Verbose)
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
}
