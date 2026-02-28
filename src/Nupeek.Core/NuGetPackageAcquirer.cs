using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.IO.Compression;

namespace Nupeek.Core;

public sealed class NuGetPackageAcquirer
{
    // Resolves, downloads, and extracts a NuGet package into local cache.
    public async Task<NuGetPackageResult> AcquireAsync(NuGetPackageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PackageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CacheRoot);

        var logger = NullLogger.Instance;
        var repositories = GetRepositories();
        var packageId = request.PackageId.Trim();
        var version = await ResolveVersionAsync(repositories, packageId, request.Version, logger, cancellationToken).ConfigureAwait(false);

        var packageDir = NuGetCacheLayout.PackageDirectory(request.CacheRoot, packageId, version);
        var nupkgPath = NuGetCacheLayout.NupkgPath(request.CacheRoot, packageId, version);
        var extractedPath = NuGetCacheLayout.ExtractedPath(request.CacheRoot, packageId, version);

        Directory.CreateDirectory(packageDir);

        // Download once; reuse local cache on subsequent runs.
        if (!File.Exists(nupkgPath))
        {
            await DownloadPackageAsync(repositories, packageId, version, nupkgPath, logger, cancellationToken).ConfigureAwait(false);
        }

        // Extract once; re-extract only when folder is missing/empty.
        if (!Directory.Exists(extractedPath) || Directory.GetFileSystemEntries(extractedPath).Length == 0)
        {
            if (Directory.Exists(extractedPath))
            {
                Directory.Delete(extractedPath, recursive: true);
            }

            ZipFile.ExtractToDirectory(nupkgPath, extractedPath);
        }

        return new NuGetPackageResult(packageId, version, packageDir, nupkgPath, extractedPath);
    }

    private static IReadOnlyList<SourceRepository> GetRepositories()
    {
        var providers = Repository.Provider.GetCoreV3();
        var packageSources = Settings.LoadDefaultSettings(root: null)
            .GetSection("packageSources")?
            .Items?
            .OfType<SourceItem>()
            .Select(x => new PackageSource(x.GetValueAsPath()))
            .ToList();

        packageSources ??= [];
        if (!packageSources.Any(static x => x.Source.Contains("nuget.org", StringComparison.OrdinalIgnoreCase)))
        {
            packageSources.Add(new PackageSource("https://api.nuget.org/v3/index.json"));
        }

        return packageSources.Select(source => new SourceRepository(source, providers)).ToList();
    }

    private static async Task<string> ResolveVersionAsync(
        IReadOnlyList<SourceRepository> repositories,
        string packageId,
        string? requestedVersion,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedVersion))
        {
            return requestedVersion.Trim();
        }

        var versions = new List<NuGetVersion>();

        foreach (var repository in repositories)
        {
            var metadata = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken).ConfigureAwait(false);
            using var cacheContext = new SourceCacheContext();
            var found = await metadata.GetMetadataAsync(packageId, includePrerelease: false, includeUnlisted: false, cacheContext, logger, cancellationToken).ConfigureAwait(false);
            versions.AddRange(found.Select(m => m.Identity.Version));
        }

        var latest = versions
            .Distinct()
            .OrderByDescending(static x => x)
            .FirstOrDefault();

        if (latest is null)
        {
            throw new InvalidOperationException($"Package '{packageId}' was not found.");
        }

        return latest.ToNormalizedString();
    }

    private static async Task DownloadPackageAsync(
        IReadOnlyList<SourceRepository> repositories,
        string packageId,
        string version,
        string destinationPath,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var identity = new PackageIdentity(packageId, NuGetVersion.Parse(version));

        foreach (var repository in repositories)
        {
            var resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken).ConfigureAwait(false);
            using var cacheContext = new SourceCacheContext();
            using var memory = new MemoryStream();

            var found = await resource.CopyNupkgToStreamAsync(
                identity.Id,
                identity.Version,
                memory,
                cacheContext,
                logger,
                cancellationToken).ConfigureAwait(false);

            if (!found)
            {
                continue;
            }

            memory.Position = 0;
            using var file = File.Create(destinationPath);
            await memory.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
            return;
        }

        throw new InvalidOperationException($"Unable to download package '{packageId}' version '{version}'.");
    }
}
