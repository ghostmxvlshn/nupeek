namespace Nupeek.Core;

/// <summary>
/// Provides deterministic cache path conventions for package artifacts.
/// </summary>
public static class NuGetCacheLayout
{
    /// <summary>
    /// Gets the package/version cache directory.
    /// </summary>
    public static string PackageDirectory(string cacheRoot, string packageId, string version)
        => Path.Combine(cacheRoot, "packages", packageId.ToLowerInvariant(), version);

    /// <summary>
    /// Gets the expected path to the downloaded <c>.nupkg</c> file.
    /// </summary>
    public static string NupkgPath(string cacheRoot, string packageId, string version)
        => Path.Combine(PackageDirectory(cacheRoot, packageId, version), $"{packageId.ToLowerInvariant()}.{version}.nupkg");

    /// <summary>
    /// Gets the extraction target directory for package contents.
    /// </summary>
    public static string ExtractedPath(string cacheRoot, string packageId, string version)
        => Path.Combine(PackageDirectory(cacheRoot, packageId, version), "extracted");
}
