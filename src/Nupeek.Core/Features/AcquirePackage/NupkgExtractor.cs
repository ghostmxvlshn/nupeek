using System.IO.Compression;

namespace Nupeek.Core;

internal static class NupkgExtractor
{
    private const int MaxZipEntries = 20_000;
    private const long MaxExtractedBytes = 1L * 1024 * 1024 * 1024;

    public static void ExtractSafely(string nupkgPath, string extractedPath)
    {
        Directory.CreateDirectory(extractedPath);

        var destinationRoot = Path.GetFullPath(extractedPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        using var archive = ZipFile.OpenRead(nupkgPath);

        if (archive.Entries.Count > MaxZipEntries)
        {
            throw new InvalidOperationException($"Package archive has too many entries ({archive.Entries.Count}).");
        }

        long totalExtracted = 0;

        foreach (var entry in archive.Entries)
        {
            var destinationPath = Path.GetFullPath(Path.Combine(extractedPath, entry.FullName));
            if (!destinationPath.StartsWith(destinationRoot, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unsafe archive entry path '{entry.FullName}'.");
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            totalExtracted += entry.Length;
            if (totalExtracted > MaxExtractedBytes)
            {
                throw new InvalidOperationException($"Package extracted size exceeds safety limit ({MaxExtractedBytes} bytes).");
            }

            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }
}
