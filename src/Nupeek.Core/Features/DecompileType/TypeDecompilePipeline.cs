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
    /// Executes package acquisition (or local assembly mode), type location, decompilation, and catalog updates.
    /// </summary>
    public async Task<TypeDecompileResult> RunAsync(TypeDecompileRequest request, CancellationToken cancellationToken = default)
    {
        ValidateSource(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputRoot);

        var source = await ResolveSourceAsync(request, cancellationToken).ConfigureAwait(false);
        var content = source.Content;

        var outputPath = await DecompileAndCatalogAsync(
            request.OutputRoot,
            source.PackageId,
            source.Version,
            content.SelectedTfm,
            content.AssemblyPath,
            content.FullTypeName,
            cancellationToken).ConfigureAwait(false);

        if (request.Depth > 0)
        {
            var related = await _locator
                .GetRelatedTypesInAssemblyAsync(content.AssemblyPath, content.FullTypeName, request.Depth, cancellationToken)
                .ConfigureAwait(false);

            foreach (var relatedType in related)
            {
                await DecompileAndCatalogAsync(
                    request.OutputRoot,
                    source.PackageId,
                    source.Version,
                    content.SelectedTfm,
                    content.AssemblyPath,
                    relatedType,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        var indexPath = Path.Combine(request.OutputRoot, "index.json");
        var manifestPath = Path.Combine(request.OutputRoot, "manifest.json");

        return new TypeDecompileResult(
            source.PackageId,
            source.Version,
            content.SelectedTfm,
            content.FullTypeName,
            content.AssemblyPath,
            outputPath,
            indexPath,
            manifestPath);
    }

    private static void ValidateSource(TypeDecompileRequest request)
    {
        var hasPackage = !string.IsNullOrWhiteSpace(request.PackageId);
        var hasAssembly = !string.IsNullOrWhiteSpace(request.AssemblyPath);

        if (hasPackage == hasAssembly)
        {
            throw new ArgumentException("Provide exactly one source: package or assembly path.", nameof(request));
        }
    }

    private async Task<(string PackageId, string Version, PackageContentResult Content)> ResolveSourceAsync(TypeDecompileRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.AssemblyPath))
        {
            var assemblyPath = request.AssemblyPath!.Trim();
            var content = await _locator.LocateInAssemblyAsync(assemblyPath, request.TypeName, cancellationToken).ConfigureAwait(false);

            var packageId = Path.GetFileNameWithoutExtension(assemblyPath).ToLowerInvariant();
            return (packageId, "local", content);
        }

        var cacheRoot = Path.Combine(request.OutputRoot, ".cache");
        var package = await _acquirer.AcquireAsync(new NuGetPackageRequest(request.PackageId!, request.Version, cacheRoot), cancellationToken).ConfigureAwait(false);
        var packageContent = await _locator.LocateAsync(new PackageContentRequest(package.ExtractedPath, request.TypeName, request.Tfm), cancellationToken).ConfigureAwait(false);

        return (package.PackageId, package.Version, packageContent);
    }

    private async Task<string> DecompileAndCatalogAsync(
        string outputRoot,
        string packageId,
        string version,
        string tfm,
        string assemblyPath,
        string fullTypeName,
        CancellationToken cancellationToken)
    {
        var outputPath = OutputPathBuilder.BuildTypeOutputPath(outputRoot, packageId, version, tfm, fullTypeName);
        await _decompiler.DecompileTypeAsync(assemblyPath, fullTypeName, outputPath, cancellationToken).ConfigureAwait(false);
        await _catalogWriter.WriteIndexAsync(outputRoot, fullTypeName, outputPath, cancellationToken).ConfigureAwait(false);
        await _catalogWriter.WriteManifestAsync(outputRoot, new ManifestEntry(
            packageId,
            version,
            tfm,
            fullTypeName,
            assemblyPath,
            outputPath,
            DateTimeOffset.UtcNow), cancellationToken).ConfigureAwait(false);

        return outputPath;
    }
}
