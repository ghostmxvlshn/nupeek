using Nupeek.Core.Models;

namespace Nupeek.Core;

public sealed class TypeDecompilePipeline
{
    // Orchestrates end-to-end decompilation for one type.
    private readonly NuGetPackageAcquirer _acquirer;
    private readonly PackageTypeLocator _locator;
    private readonly TypeDecompiler _decompiler;
    private readonly OutputCatalogWriter _catalogWriter;

    public TypeDecompilePipeline()
        : this(new NuGetPackageAcquirer(), new PackageTypeLocator(), new TypeDecompiler(), new OutputCatalogWriter())
    {
    }

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

    public async Task<TypeDecompileResult> RunAsync(TypeDecompileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PackageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputRoot);

        // Keep package artifacts in a deterministic cache under output root.
        var cacheRoot = Path.Combine(request.OutputRoot, ".cache");

        // 1) Acquire and extract package.
        var package = await _acquirer.AcquireAsync(new NuGetPackageRequest(request.PackageId, request.Version, cacheRoot), cancellationToken).ConfigureAwait(false);

        // 2) Resolve TFM/lib and locate the assembly containing the target type.
        var content = _locator.Locate(new PackageContentRequest(
            package.ExtractedPath,
            request.TypeName,
            request.Tfm));

        var outputPath = OutputPathBuilder.BuildTypeOutputPath(
            request.OutputRoot,
            package.PackageId,
            package.Version,
            content.SelectedTfm,
            request.TypeName);

        // 3) Decompile the type into a stable output location.
        _decompiler.DecompileType(content.AssemblyPath, request.TypeName, outputPath);

        // 4) Update machine-readable catalogs for fast lookup and provenance.
        var indexPath = _catalogWriter.WriteIndex(request.OutputRoot, request.TypeName, outputPath);
        var manifestPath = _catalogWriter.WriteManifest(request.OutputRoot, new ManifestEntry(
            package.PackageId,
            package.Version,
            content.SelectedTfm,
            request.TypeName,
            content.AssemblyPath,
            outputPath,
            DateTimeOffset.UtcNow));

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
