using Nupeek.Core;

namespace Nupeek.Cli;

internal static class RunPlanRealExecution
{
    public static async Task<CliOutcome> ExecuteAsync(PlanRequest request, string emit, int maxChars, CancellationToken cancellationToken)
    {
        try
        {
            var pipeline = new TypeDecompilePipeline();
            var result = await pipeline.RunAsync(new TypeDecompileRequest(
                request.Package,
                string.Equals(request.Version, "latest", StringComparison.OrdinalIgnoreCase) ? null : request.Version,
                string.Equals(request.Tfm, "auto", StringComparison.OrdinalIgnoreCase) ? null : request.Tfm,
                request.Type,
                request.OutDir), cancellationToken).ConfigureAwait(false);

            var inlineSource = await InlineSourceReader
                .ReadInlineSourceAsync(result.OutputPath, emit, maxChars, cancellationToken)
                .ConfigureAwait(false);

            return new CliOutcome(
                ExitCodes.Success,
                null,
                result.PackageId,
                result.Version,
                result.Tfm,
                result.AssemblyPath,
                result.OutputPath,
                result.IndexPath,
                result.ManifestPath,
                inlineSource.Content,
                inlineSource.MaxChars,
                inlineSource.OriginalChars,
                inlineSource.Truncated);
        }
        catch (OperationCanceledException)
        {
            return new CliOutcome(ExitCodes.OperationCanceled, "Operation canceled.", request.Package, request.Version, request.Tfm, null, null, null, null, null, null, null, false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return new CliOutcome(ExitCodes.TypeOrSymbolNotFound, ex.Message, request.Package, request.Version, request.Tfm, null, null, null, null, null, null, null, false);
        }
        catch (InvalidOperationException ex)
        {
            return new CliOutcome(ExitCodes.PackageResolutionFailure, ex.Message, request.Package, request.Version, request.Tfm, null, null, null, null, null, null, null, false);
        }
        catch (Exception ex)
        {
            return new CliOutcome(ExitCodes.DecompilationFailure, $"Decompilation failed: {ex.Message}", request.Package, request.Version, request.Tfm, null, null, null, null, null, null, null, false);
        }
    }
}
