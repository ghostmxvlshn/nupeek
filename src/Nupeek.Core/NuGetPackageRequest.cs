namespace Nupeek.Core;

public sealed record NuGetPackageRequest(
    string PackageId,
    string? Version,
    string CacheRoot);
