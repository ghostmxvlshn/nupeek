using Nupeek.Core.Models;

namespace Nupeek.Core;

/// <summary>
/// Orchestrates end-to-end decompilation flow for one type request.
/// </summary>
public sealed class TypeDecompilePipeline
{
    private readonly NuGetPackageAcquirer _acquirer;
    private readonly PackageTypeLocator _locator;
    private readonly TypeDecompiler _decompiler;
    private readonly OutputCatalogWriter _catalogWriter;

    /// <summary>
    /// Creates pipeline with default concrete services.
    /// </summary>
    public TypeDecompilePipeline()
        : this(new NuGetPackageAcquirer(), new PackageTypeLocator(), new TypeDecompiler(), new OutputCatalogWriter())
    {
    }

    /// <summary>
    /// Creates pipeline with explicit service dependencies.
    /// </summary>
    public TypeDecompilePipeline(
        NuGetPackageAcquirer acquirer,
        PackageTypeLocator locator,
        TypeDecompiler decompiler,
        OutputCatalogWriter catalogWriter)
    {
        _acquirer = acquirer;
        _locator = locator;
        _decompiler = decompiler;
        _catalogWriter = catalogWriter;
    }

    /// <summary>
    /// Executes package acquisition, type location, decompilation, and catalog updates.
    /// </summary>
    public async Task<TypeDecompileResult> RunAsync(TypeDecompileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PackageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputRoot);

        // Keep package artifacts under output root so the run is self-contained.
        var cacheRoot = Path.Combine(request.OutputRoot, ".cache");

        // 1) Acquire package and extracted content.
        var package = await _acquirer.AcquireAsync(new NuGetPackageRequest(request.PackageId, request.Version, cacheRoot), cancellationToken).ConfigureAwait(false);

        // 2) Resolve lib/TFM and assembly containing target type.
        var content = _locator.Locate(new PackageContentRequest(
            package.ExtractedPath,
            request.TypeName,
            request.Tfm));

        // 3) Compute deterministic output file path.
        var outputPath = OutputPathBuilder.BuildTypeOutputPath(
            request.OutputRoot,
            package.PackageId,
            package.Version,
            content.SelectedTfm,
            request.TypeName);

        // 4) Decompile target type into generated C# file.
        await _decompiler.DecompileTypeAsync(content.AssemblyPath, request.TypeName, outputPath, cancellationToken).ConfigureAwait(false);

        // 5) Update index and manifest for downstream tooling.
        var indexPath = await _catalogWriter.WriteIndexAsync(request.OutputRoot, request.TypeName, outputPath, cancellationToken).ConfigureAwait(false);
        var manifestPath = await _catalogWriter.WriteManifestAsync(request.OutputRoot, new ManifestEntry(
            package.PackageId,
            package.Version,
            content.SelectedTfm,
            request.TypeName,
            content.AssemblyPath,
            outputPath,
            DateTimeOffset.UtcNow), cancellationToken).ConfigureAwait(false);

        return new TypeDecompileResult(
            package.PackageId,
            package.Version,
            content.SelectedTfm,
            request.TypeName,
            content.AssemblyPath,
            outputPath,
            indexPath,
            manifestPath);
    }
}
