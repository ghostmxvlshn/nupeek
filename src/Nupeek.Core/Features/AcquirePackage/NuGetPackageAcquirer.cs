using NuGet.Common;

namespace Nupeek.Core;

/// <summary>
/// Resolves, downloads, and extracts NuGet packages into deterministic local cache.
/// </summary>
public sealed class NuGetPackageAcquirer
{
    /// <summary>
    /// Acquires package content in local cache and returns resolved paths/metadata.
    /// </summary>
    public async Task<NuGetPackageResult> AcquireAsync(NuGetPackageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PackageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CacheRoot);

        var packageId = request.PackageId.Trim();
        NuGetPackageValidation.ValidatePackageId(packageId);

        if (!string.IsNullOrWhiteSpace(request.Version))
        {
            NuGetPackageValidation.ValidateVersion(request.Version.Trim());
        }

        var logger = NullLogger.Instance;
        var repositories = NuGetSourceRepositoryFactory.Create();

        var version = await NuGetVersionResolver.ResolveAsync(
            repositories,
            packageId,
            request.Version,
            logger,
            cancellationToken).ConfigureAwait(false);

        var packageDir = NuGetCacheLayout.PackageDirectory(request.CacheRoot, packageId, version);
        var nupkgPath = NuGetCacheLayout.NupkgPath(request.CacheRoot, packageId, version);
        var extractedPath = NuGetCacheLayout.ExtractedPath(request.CacheRoot, packageId, version);

        Directory.CreateDirectory(packageDir);

        if (!File.Exists(nupkgPath))
        {
            await NuGetPackageDownloader.DownloadAsync(
                repositories,
                packageId,
                version,
                nupkgPath,
                logger,
                cancellationToken).ConfigureAwait(false);
        }

        if (!Directory.Exists(extractedPath) || Directory.GetFileSystemEntries(extractedPath).Length == 0)
        {
            if (Directory.Exists(extractedPath))
            {
                Directory.Delete(extractedPath, recursive: true);
            }

            NupkgExtractor.ExtractSafely(nupkgPath, extractedPath);
        }

        return new NuGetPackageResult(packageId, version, packageDir, nupkgPath, extractedPath);
    }
}
