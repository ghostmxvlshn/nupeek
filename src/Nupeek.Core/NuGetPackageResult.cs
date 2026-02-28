namespace Nupeek.Core;

public sealed record NuGetPackageResult(
    string PackageId,
    string Version,
    string PackageDirectory,
    string NupkgPath,
    string ExtractedPath);
