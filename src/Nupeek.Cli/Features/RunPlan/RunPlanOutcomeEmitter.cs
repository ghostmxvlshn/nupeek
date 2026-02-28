using System.Text.Json;

namespace Nupeek.Cli;

internal static class RunPlanOutcomeEmitter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static void Emit(CliOutcome outcome, PlanRequest request, string format, string emit)
    {
        if (string.Equals(format, "json", StringComparison.Ordinal))
        {
            EmitJson(outcome, request, emit);
            return;
        }

        if (outcome.ExitCode != ExitCodes.Success)
        {
            Console.Error.WriteLine(outcome.Error);
            return;
        }

        if (request.Quiet)
        {
            return;
        }

        EmitTextPlan(request);

        if (!request.DryRun)
        {
            EmitGeneratedPaths(outcome);
            EmitInlineSourceIfNeeded(outcome, emit);
        }
    }

    private static void EmitJson(CliOutcome outcome, PlanRequest request, string emit)
    {
        WriteJson(new CliRunResult(
            request.Command,
            outcome.PackageId,
            outcome.Version,
            outcome.SelectedTfm,
            string.Equals(request.Command, "type", StringComparison.Ordinal) ? request.Type : null,
            request.SourceSymbol,
            request.Type,
            outcome.AssemblyPath,
            outcome.OutputPath,
            outcome.IndexPath,
            outcome.ManifestPath,
            emit,
            outcome.InlineSource,
            outcome.MaxChars,
            outcome.OriginalChars,
            outcome.Truncated,
            request.DryRun,
            outcome.ExitCode,
            outcome.Error));
    }

    private static void EmitTextPlan(PlanRequest request)
    {
        Console.WriteLine(RunPlanTextBuilder.Build(
            request.Command,
            request.Package,
            request.Version,
            request.Tfm,
            request.Type,
            request.OutDir,
            request.DryRun,
            request.SourceSymbol));
    }

    private static void EmitGeneratedPaths(CliOutcome outcome)
    {
        Console.WriteLine($"outputPath: {outcome.OutputPath}");
        Console.WriteLine($"indexPath: {outcome.IndexPath}");
        Console.WriteLine($"manifestPath: {outcome.ManifestPath}");
    }

    private static void EmitInlineSourceIfNeeded(CliOutcome outcome, string emit)
    {
        if (!string.Equals(emit, "agent", StringComparison.Ordinal) || string.IsNullOrEmpty(outcome.InlineSource))
        {
            return;
        }

        Console.WriteLine("--- inlineSource:start ---");
        Console.WriteLine(outcome.InlineSource);
        Console.WriteLine("--- inlineSource:end ---");

        if (outcome.Truncated)
        {
            Console.WriteLine($"inlineSourceTruncated: true ({outcome.MaxChars}/{outcome.OriginalChars} chars)");
        }
    }

    private static void WriteJson(CliRunResult payload)
    {
        Console.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
