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

        var requestedTypeName = TypeNameNormalizer.Normalize(request.FullTypeName);
        var selectedLibDir = SelectLibraryDirectory(request.ExtractedPath, request.Tfm);
        var selectedTfm = Path.GetFileName(selectedLibDir);

        var exact = FindExactType(selectedLibDir, requestedTypeName);
        if (exact is not null)
        {
            return new PackageContentResult(selectedTfm, selectedLibDir, exact.Value.AssemblyPath, exact.Value.TypeName);
        }

        return ResolveByMemberFallback(request.FullTypeName, requestedTypeName, selectedLibDir, selectedTfm);
    }

    private static PackageContentResult ResolveByMemberFallback(string originalSymbol, string requestedTypeName, string selectedLibDir, string selectedTfm)
    {
        var memberName = SymbolParser.ExtractMemberName(originalSymbol);
        var candidateTypes = FindDeclaringTypesForMemberName(selectedLibDir, memberName);

        if (candidateTypes.Count == 1)
        {
            var hit = candidateTypes[0];
            return new PackageContentResult(selectedTfm, selectedLibDir, hit.AssemblyPath, hit.TypeName);
        }

        if (candidateTypes.Count > 1)
        {
            var suggestions = string.Join(", ",
                candidateTypes
                    .Select(static x => x.TypeName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(static x => x, StringComparer.Ordinal)
                    .Take(5));

            throw new InvalidOperationException(
                $"Type '{requestedTypeName}' was not found in '{selectedLibDir}'. Found member '{memberName}' in multiple types: {suggestions}. Use --type with a fully-qualified type name.");
        }

        throw new InvalidOperationException($"Type '{requestedTypeName}' was not found in '{selectedLibDir}'.");
    }

    private static string SelectLibraryDirectory(string extractedPath, string? tfm)
    {
        var libRoot = Path.Combine(extractedPath, "lib");
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

        var selectedTfm = string.IsNullOrWhiteSpace(tfm)
            ? TfmSelector.SelectBest(tfmDirectories)
            : tfm.Trim();

        var selectedLibDir = Path.Combine(libRoot, selectedTfm);
        if (!Directory.Exists(selectedLibDir))
        {
            throw new InvalidOperationException($"Requested TFM '{selectedTfm}' not found in package.");
        }

        return selectedLibDir;
    }

    private static (string AssemblyPath, string TypeName)? FindExactType(string libDir, string fullTypeName)
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
                    var typeName = GetTypeFullName(md, handle);
                    if (string.Equals(typeName, fullTypeName, StringComparison.Ordinal))
                    {
                        return (dll, typeName);
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

    private static List<(string AssemblyPath, string TypeName)> FindDeclaringTypesForMemberName(string libDir, string memberName)
    {
        var matches = new List<(string AssemblyPath, string TypeName)>();

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
                    var typeName = GetTypeFullName(md, handle);

                    if (HasMethod(typeDef, md, memberName)
                        || HasProperty(typeDef, md, memberName)
                        || HasField(typeDef, md, memberName)
                        || HasEvent(typeDef, md, memberName))
                    {
                        matches.Add((dll, typeName));
                    }
                }
            }
            catch
            {
                // Ignore unreadable/unmanaged assemblies and continue scanning.
            }
        }

        return matches
            .DistinctBy(static x => (x.AssemblyPath, x.TypeName))
            .ToList();
    }

    private static bool HasMethod(TypeDefinition typeDef, MetadataReader md, string memberName)
        => typeDef.GetMethods()
            .Select(md.GetMethodDefinition)
            .Any(method => string.Equals(md.GetString(method.Name), memberName, StringComparison.Ordinal));

    private static bool HasProperty(TypeDefinition typeDef, MetadataReader md, string memberName)
        => typeDef.GetProperties()
            .Select(md.GetPropertyDefinition)
            .Any(property => string.Equals(md.GetString(property.Name), memberName, StringComparison.Ordinal));

    private static bool HasField(TypeDefinition typeDef, MetadataReader md, string memberName)
        => typeDef.GetFields()
            .Select(md.GetFieldDefinition)
            .Any(field => string.Equals(md.GetString(field.Name), memberName, StringComparison.Ordinal));

    private static bool HasEvent(TypeDefinition typeDef, MetadataReader md, string memberName)
        => typeDef.GetEvents()
            .Select(md.GetEventDefinition)
            .Any(eventDef => string.Equals(md.GetString(eventDef.Name), memberName, StringComparison.Ordinal));

    private static string GetTypeFullName(MetadataReader md, TypeDefinitionHandle handle)
    {
        var typeDef = md.GetTypeDefinition(handle);
        var ns = md.GetString(typeDef.Namespace);
        var name = md.GetString(typeDef.Name);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }
}
