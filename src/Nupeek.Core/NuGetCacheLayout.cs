namespace Nupeek.Core;

public static class NuGetCacheLayout
{
    public static string PackageDirectory(string cacheRoot, string packageId, string version)
        => Path.Combine(cacheRoot, "packages", packageId.ToLowerInvariant(), version);

    public static string NupkgPath(string cacheRoot, string packageId, string version)
        => Path.Combine(PackageDirectory(cacheRoot, packageId, version), $"{packageId.ToLowerInvariant()}.{version}.nupkg");

    public static string ExtractedPath(string cacheRoot, string packageId, string version)
        => Path.Combine(PackageDirectory(cacheRoot, packageId, version), "extracted");
}
