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
    {
        var indexPath = Path.Combine(outputRoot, "index.json");
        Directory.CreateDirectory(outputRoot);

        // Read existing index if present, otherwise start a new map.
        var index = ReadJson<TypeIndex>(indexPath) ?? new TypeIndex();

        // Last write wins for the same type.
        index[typeName] = outputPath;

        File.WriteAllText(indexPath, JsonSerializer.Serialize(index, JsonOptions));
        return indexPath;
    }

    /// <summary>
    /// Upserts manifest entry in <c>manifest.json</c>.
    /// </summary>
    public string WriteManifest(string outputRoot, ManifestEntry entry)
    {
        var manifestPath = Path.Combine(outputRoot, "manifest.json");
        Directory.CreateDirectory(outputRoot);

        // Read existing manifest list if present.
        var manifest = ReadJson<List<ManifestEntry>>(manifestPath) ?? [];

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

        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions));
        return manifestPath;
    }

    /// <summary>
    /// Reads JSON file into target type. Returns default when file is missing/empty.
    /// </summary>
    private static T? ReadJson<T>(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}
