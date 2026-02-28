namespace Nupeek.Core;

/// <summary>
/// Builds deterministic file paths for generated decompiled type output.
/// </summary>
public static class OutputPathBuilder
{
    /// <summary>
    /// Builds the output file path for a single decompiled type.
    /// </summary>
    public static string BuildTypeOutputPath(string root, string packageId, string version, string tfm, string fullTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(tfm);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullTypeName);

        // Keep filename human-readable while avoiding invalid filesystem characters.
        var fileName = SanitizeFileName(fullTypeName.Replace('.', '_')) + ".decompiled.cs";

        // Layout is intentionally stable for indexing and repeat runs.
        return Path.Combine(root, "packages", packageId.ToLowerInvariant(), version, tfm, fileName);
    }

    /// <summary>
    /// Replaces unsupported characters with underscores for safe file names.
    /// </summary>
    private static string SanitizeFileName(string value)
    {
        var chars = value
            .Select(c => char.IsLetterOrDigit(c) || c is '_' or '.' or '-' ? c : '_')
            .ToArray();

        return new string(chars);
    }
}
