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

        var outputPath = OutputPathBuilder.BuildTypeOutputPath(
            request.OutputRoot,
            source.PackageId,
            source.Version,
            content.SelectedTfm,
            content.FullTypeName);

        await _decompiler.DecompileTypeAsync(content.AssemblyPath, content.FullTypeName, outputPath, cancellationToken).ConfigureAwait(false);

        var indexPath = await _catalogWriter.WriteIndexAsync(request.OutputRoot, content.FullTypeName, outputPath, cancellationToken).ConfigureAwait(false);
        var manifestPath = await _catalogWriter.WriteManifestAsync(request.OutputRoot, new ManifestEntry(
            source.PackageId,
            source.Version,
            content.SelectedTfm,
            content.FullTypeName,
            content.AssemblyPath,
            outputPath,
            DateTimeOffset.UtcNow), cancellationToken).ConfigureAwait(false);

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
            var content = _locator.LocateInAssembly(assemblyPath, request.TypeName);

            var packageId = Path.GetFileNameWithoutExtension(assemblyPath).ToLowerInvariant();
            return (packageId, "local", content);
        }

        var cacheRoot = Path.Combine(request.OutputRoot, ".cache");
        var package = await _acquirer.AcquireAsync(new NuGetPackageRequest(request.PackageId!, request.Version, cacheRoot), cancellationToken).ConfigureAwait(false);
        var packageContent = _locator.Locate(new PackageContentRequest(package.ExtractedPath, request.TypeName, request.Tfm));

        return (package.PackageId, package.Version, packageContent);
    }
}
