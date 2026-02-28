using Nupeek.Core.Models;

namespace Nupeek.Core;

public sealed class TypeDecompilePipeline
{
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

        var cacheRoot = Path.Combine(request.OutputRoot, ".cache");

        var package = await _acquirer.AcquireAsync(new NuGetPackageRequest(request.PackageId, request.Version, cacheRoot), cancellationToken).ConfigureAwait(false);

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

        _decompiler.DecompileType(content.AssemblyPath, request.TypeName, outputPath);

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
