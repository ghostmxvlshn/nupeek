namespace Nupeek.Cli;

internal static class RunPlanSourceLabel
{
    public static string Get(PlanRequest request)
        => !string.IsNullOrWhiteSpace(request.Package)
            ? request.Package!
            : request.Assembly ?? "assembly";
}
