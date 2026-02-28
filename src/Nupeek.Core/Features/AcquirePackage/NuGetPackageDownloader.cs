using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Nupeek.Core;

internal static class NuGetPackageDownloader
{
    public static async Task DownloadAsync(
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
