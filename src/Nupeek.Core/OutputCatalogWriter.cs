using Nupeek.Core.Models;
using System.Text.Json;

namespace Nupeek.Core;

/// <summary>
/// Reads and updates output catalogs (<c>index.json</c> and <c>manifest.json</c>).
/// </summary>
public sealed class OutputCatalogWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Upserts type-to-output-path mapping in <c>index.json</c>.
    /// </summary>
    public string WriteIndex(string outputRoot, string typeName, string outputPath)
        => WriteIndexAsync(outputRoot, typeName, outputPath).GetAwaiter().GetResult();

    /// <summary>
    /// Upserts type-to-output-path mapping in <c>index.json</c> (async).
    /// </summary>
    public async Task<string> WriteIndexAsync(string outputRoot, string typeName, string outputPath, CancellationToken cancellationToken = default)
    {
        var indexPath = Path.Combine(outputRoot, "index.json");
        Directory.CreateDirectory(outputRoot);

        // Read existing index if present, otherwise start a new map.
        var index = await ReadJsonAsync<TypeIndex>(indexPath, cancellationToken).ConfigureAwait(false) ?? new TypeIndex();

        // Last write wins for the same type.
        index[typeName] = outputPath;

        await File.WriteAllTextAsync(indexPath, JsonSerializer.Serialize(index, JsonOptions), cancellationToken).ConfigureAwait(false);
        return indexPath;
    }

    /// <summary>
    /// Upserts manifest entry in <c>manifest.json</c>.
    /// </summary>
    public string WriteManifest(string outputRoot, ManifestEntry entry)
        => WriteManifestAsync(outputRoot, entry).GetAwaiter().GetResult();

    /// <summary>
    /// Upserts manifest entry in <c>manifest.json</c> (async).
    /// </summary>
    public async Task<string> WriteManifestAsync(string outputRoot, ManifestEntry entry, CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(outputRoot, "manifest.json");
        Directory.CreateDirectory(outputRoot);

        // Read existing manifest list if present.
        var manifest = await ReadJsonAsync<List<ManifestEntry>>(manifestPath, cancellationToken).ConfigureAwait(false) ?? [];

        // Unique key: package/version/tfm/type.
        var existingIndex = manifest.FindIndex(x =>
            string.Equals(x.PackageId, entry.PackageId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Version, entry.Version, StringComparison.Ordinal)
            && string.Equals(x.Tfm, entry.Tfm, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.TypeName, entry.TypeName, StringComparison.Ordinal));

        if (existingIndex >= 0)
        {
            manifest[existingIndex] = entry;
        }
        else
        {
            manifest.Add(entry);
        }

        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions), cancellationToken).ConfigureAwait(false);
        return manifestPath;
    }

    /// <summary>
    /// Reads JSON file into target type. Returns default when file is missing/empty.
    /// </summary>
    private static T? ReadJson<T>(string path)
        => ReadJsonAsync<T>(path).GetAwaiter().GetResult();

    /// <summary>
    /// Reads JSON file into target type (async). Returns default when file is missing/empty.
    /// </summary>
    private static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}
