using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Nupeek.Core;

/// <summary>
/// Resolves TFM/lib directory and locates assembly containing a target type.
/// </summary>
public sealed class PackageTypeLocator
{
    /// <summary>
    /// Locates package content (TFM/lib/assembly) for the requested type.
    /// </summary>
    public PackageContentResult Locate(PackageContentRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ExtractedPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FullTypeName);

        // NuGet library binaries are expected under extracted/lib/<tfm>/.
        var libRoot = Path.Combine(request.ExtractedPath, "lib");
        if (!Directory.Exists(libRoot))
        {
            throw new InvalidOperationException($"Package does not contain a lib folder: {libRoot}");
        }

        // Enumerate available TFMs in package.
        var tfmDirectories = Directory.GetDirectories(libRoot)
            .Select(Path.GetFileName)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();

        if (tfmDirectories.Count == 0)
        {
            throw new InvalidOperationException($"No target frameworks found under: {libRoot}");
        }

        // Respect explicit TFM; otherwise auto-select best candidate.
        var selectedTfm = string.IsNullOrWhiteSpace(request.Tfm)
            ? TfmSelector.SelectBest(tfmDirectories)
            : request.Tfm.Trim();

        var selectedLibDir = Path.Combine(libRoot, selectedTfm);
        if (!Directory.Exists(selectedLibDir))
        {
            throw new InvalidOperationException($"Requested TFM '{selectedTfm}' not found in package.");
        }

        // Metadata scan each managed assembly to find the declaring type.
        var assemblyPath = FindAssemblyContainingType(selectedLibDir, request.FullTypeName)
            ?? throw new InvalidOperationException($"Type '{request.FullTypeName}' was not found in '{selectedLibDir}'.");

        return new PackageContentResult(selectedTfm, selectedLibDir, assemblyPath, request.FullTypeName);
    }

    /// <summary>
    /// Returns first assembly path that defines <paramref name="fullTypeName"/>.
    /// </summary>
    private static string? FindAssemblyContainingType(string libDir, string fullTypeName)
    {
        foreach (var dll in Directory.GetFiles(libDir, "*.dll"))
        {
            try
            {
                using var stream = File.OpenRead(dll);
                using var peReader = new PEReader(stream);

                // Skip native or invalid binaries without CLI metadata.
                if (!peReader.HasMetadata)
                {
                    continue;
                }

                var md = peReader.GetMetadataReader();
                foreach (var handle in md.TypeDefinitions)
                {
                    var typeDef = md.GetTypeDefinition(handle);
                    var ns = md.GetString(typeDef.Namespace);
                    var name = md.GetString(typeDef.Name);
                    var candidate = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";

                    if (string.Equals(candidate, fullTypeName, StringComparison.Ordinal))
                    {
                        return dll;
                    }
                }
            }
            catch
            {
                // Ignore unreadable/unmanaged assemblies and continue scanning.
            }
        }

        return null;
    }
}
