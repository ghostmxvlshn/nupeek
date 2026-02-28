using NuGet.Versioning;

namespace Nupeek.Core;

internal static class NuGetPackageValidation
{
    public static void ValidatePackageId(string packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId)
            || packageId.Contains("..", StringComparison.Ordinal)
            || packageId.IndexOfAny(['/', '\\']) >= 0
            || packageId.Any(static c => !(char.IsLetterOrDigit(c) || c is '.' or '-' or '_')))
        {
            throw new ArgumentException($"Invalid package id '{packageId}'.", nameof(packageId));
        }
    }

    public static void ValidateVersion(string version)
    {
        if (!NuGetVersion.TryParse(version, out _))
        {
            throw new ArgumentException($"Invalid package version '{version}'.", nameof(version));
        }
    }
}
