using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Nupeek.Core;

public sealed class PackageTypeLocator
{
    // Resolves the best lib/TFM directory and finds the assembly that defines a target type.
    public PackageContentResult Locate(PackageContentRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ExtractedPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FullTypeName);

        var libRoot = Path.Combine(request.ExtractedPath, "lib");
        if (!Directory.Exists(libRoot))
        {
            throw new InvalidOperationException($"Package does not contain a lib folder: {libRoot}");
        }

        var tfmDirectories = Directory.GetDirectories(libRoot)
            .Select(Path.GetFileName)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();

        if (tfmDirectories.Count == 0)
        {
            throw new InvalidOperationException($"No target frameworks found under: {libRoot}");
        }

        // If user does not specify TFM, pick best known target for current runtime/tooling.
        var selectedTfm = string.IsNullOrWhiteSpace(request.Tfm)
            ? TfmSelector.SelectBest(tfmDirectories)
            : request.Tfm.Trim();

        var selectedLibDir = Path.Combine(libRoot, selectedTfm);
        if (!Directory.Exists(selectedLibDir))
        {
            throw new InvalidOperationException($"Requested TFM '{selectedTfm}' not found in package.");
        }

        var assemblyPath = FindAssemblyContainingType(selectedLibDir, request.FullTypeName)
            ?? throw new InvalidOperationException($"Type '{request.FullTypeName}' was not found in '{selectedLibDir}'.");

        return new PackageContentResult(selectedTfm, selectedLibDir, assemblyPath, request.FullTypeName);
    }

    private static string? FindAssemblyContainingType(string libDir, string fullTypeName)
    {
        foreach (var dll in Directory.GetFiles(libDir, "*.dll"))
        {
            try
            {
                using var stream = File.OpenRead(dll);
                using var peReader = new PEReader(stream);

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
                // Skip non-managed or unreadable binaries.
            }
        }

        return null;
    }
}
