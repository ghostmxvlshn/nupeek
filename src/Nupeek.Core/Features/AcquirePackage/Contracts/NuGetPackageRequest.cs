namespace Nupeek.Core;

/// <summary>
/// Input for acquiring a NuGet package into local cache.
/// </summary>
/// <param name="PackageId">Package identifier (for example, <c>Humanizer.Core</c>).</param>
/// <param name="Version">Optional explicit package version. If null, latest stable is selected.</param>
/// <param name="CacheRoot">Root cache folder where package artifacts are stored.</param>
public sealed record NuGetPackageRequest(
    string PackageId,
    string? Version,
    string CacheRoot);
