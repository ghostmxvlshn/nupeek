namespace Nupeek.Core;

public static class OutputPathBuilder
{
    public static string BuildTypeOutputPath(string root, string packageId, string version, string tfm, string fullTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(tfm);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullTypeName);

        var fileName = SanitizeFileName(fullTypeName.Replace('.', '_')) + ".decompiled.cs";
        return Path.Combine(root, "packages", packageId.ToLowerInvariant(), version, tfm, fileName);
    }

    private static string SanitizeFileName(string value)
    {
        var chars = value
            .Select(c => char.IsLetterOrDigit(c) || c is '_' or '.' or '-' ? c : '_')
            .ToArray();

        return new string(chars);
    }
}
