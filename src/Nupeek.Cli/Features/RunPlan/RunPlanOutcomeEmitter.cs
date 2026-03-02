namespace Nupeek.Cli;

internal static class RunPlanOutcomeEmitter
{
    public static void Emit(CliOutcome outcome, PlanRequest request)
    {
        if (outcome.ExitCode != ExitCodes.Success)
        {
            Console.Error.WriteLine(outcome.Error);
            return;
        }

        if (request.Quiet)
        {
            return;
        }

        Console.WriteLine(RunPlanTextBuilder.Build(
            request.Command,
            RunPlanSourceLabel.Get(request),
            request.Version,
            request.Tfm,
            request.Type,
            request.OutDir,
            request.DryRun,
            request.SourceSymbol,
            request.Depth));

        if (!request.DryRun)
        {
            Console.WriteLine($"outputPath: {outcome.OutputPath}");
            Console.WriteLine($"indexPath: {outcome.IndexPath}");
            Console.WriteLine($"manifestPath: {outcome.ManifestPath}");
        }
    }
}
