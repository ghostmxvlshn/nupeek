namespace Nupeek.Core;

/// <summary>
/// Result of NuGet package acquisition and extraction.
/// </summary>
/// <param name="PackageId">Resolved package id.</param>
/// <param name="Version">Resolved package version.</param>
/// <param name="PackageDirectory">Deterministic cache folder for this package/version.</param>
/// <param name="NupkgPath">Full path to downloaded <c>.nupkg</c> file.</param>
/// <param name="ExtractedPath">Full path to extracted package contents.</param>
public sealed record NuGetPackageResult(
    string PackageId,
    string Version,
    string PackageDirectory,
    string NupkgPath,
    string ExtractedPath);
