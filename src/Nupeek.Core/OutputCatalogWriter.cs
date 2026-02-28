using Nupeek.Core.Models;
using System.Text.Json;

namespace Nupeek.Core;

public sealed class OutputCatalogWriter
{
    // Maintains index/manifest files used by agents and follow-up CLI commands.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public string WriteIndex(string outputRoot, string typeName, string outputPath)
    {
        var indexPath = Path.Combine(outputRoot, "index.json");
        Directory.CreateDirectory(outputRoot);

        var index = ReadJson<TypeIndex>(indexPath) ?? new TypeIndex();
        index[typeName] = outputPath;

        File.WriteAllText(indexPath, JsonSerializer.Serialize(index, JsonOptions));
        return indexPath;
    }

    public string WriteManifest(string outputRoot, ManifestEntry entry)
    {
        var manifestPath = Path.Combine(outputRoot, "manifest.json");
        Directory.CreateDirectory(outputRoot);

        var manifest = ReadJson<List<ManifestEntry>>(manifestPath) ?? [];
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
