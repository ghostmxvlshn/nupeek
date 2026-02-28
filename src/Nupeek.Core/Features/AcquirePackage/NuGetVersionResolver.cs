using NuGet.Common;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Nupeek.Core;

internal static class NuGetVersionResolver
{
    public static async Task<string> ResolveAsync(
        IReadOnlyList<SourceRepository> repositories,
        string packageId,
        string? requestedVersion,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedVersion))
        {
            var normalized = requestedVersion.Trim();
            NuGetPackageValidation.ValidateVersion(normalized);
            return normalized;
        }

        var versions = new List<NuGetVersion>();

        foreach (var repository in repositories)
        {
            var metadata = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken).ConfigureAwait(false);
            using var cacheContext = new SourceCacheContext();
            var found = await metadata
                .GetMetadataAsync(packageId, includePrerelease: false, includeUnlisted: false, cacheContext, logger, cancellationToken)
                .ConfigureAwait(false);

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
}
