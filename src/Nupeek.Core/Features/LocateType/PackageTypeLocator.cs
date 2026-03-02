using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Nupeek.Core;

/// <summary>
/// Resolves TFM/lib directory and locates assembly containing a target type.
/// </summary>
public sealed class PackageTypeLocator
{
    /// <summary>
    /// Lists all type names available in a local assembly.
    /// </summary>
    public IReadOnlyList<string> ListTypesInAssembly(string assemblyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);

        if (!File.Exists(assemblyPath))
        {
            throw new InvalidOperationException($"Assembly was not found: {assemblyPath}");
        }

        return ReadTypeNamesFromAssembly(assemblyPath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Lists all type names available in a package lib directory for selected TFM.
    /// </summary>
    public IReadOnlyList<string> ListTypesInPackage(string extractedPath, string? tfm)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extractedPath);

        var selectedLibDir = SelectLibraryDirectory(extractedPath, tfm);
        return ReadTypeNamesFromDirectory(selectedLibDir)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Locates target type directly inside a specific assembly path.
    /// </summary>
    public PackageContentResult LocateInAssembly(string assemblyPath, string fullTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullTypeName);

        if (!File.Exists(assemblyPath))
        {
            throw new InvalidOperationException($"Assembly was not found: {assemblyPath}");
        }

        var normalizedType = TypeNameNormalizer.Normalize(fullTypeName);
        var dir = Path.GetDirectoryName(Path.GetFullPath(assemblyPath)) ?? ".";

        var exact = FindTypeInSingleAssembly(assemblyPath, normalizedType);
        if (!string.IsNullOrWhiteSpace(exact))
        {
            return new PackageContentResult("assembly", dir, assemblyPath, exact);
        }

        var fuzzy = FindTypeCandidatesInAssembly(assemblyPath, normalizedType);
        if (fuzzy.Count == 1)
        {
            return new PackageContentResult("assembly", dir, assemblyPath, fuzzy[0]);
        }

        if (fuzzy.Count > 1)
        {
            throw BuildAmbiguousTypeException(normalizedType, assemblyPath, fuzzy);
        }

        var memberName = SymbolParser.ExtractMemberName(fullTypeName);
        var memberTypes = FindDeclaringTypesForMemberNameInAssembly(assemblyPath, memberName);

        if (memberTypes.Count == 1)
        {
            return new PackageContentResult("assembly", dir, assemblyPath, memberTypes[0]);
        }

        if (memberTypes.Count > 1)
        {
            var suggestions = string.Join(", ", memberTypes.OrderBy(static x => x, StringComparer.Ordinal).Take(5));
            throw new InvalidOperationException(
                $"Type '{normalizedType}' was not found in '{assemblyPath}'. Found member '{memberName}' in multiple types: {suggestions}. Use --type with a fully-qualified type name. Try 'nupeek list --assembly {assemblyPath}'.");
        }

        throw new InvalidOperationException($"Type '{normalizedType}' was not found in '{assemblyPath}'. Try 'nupeek list --assembly {assemblyPath}'.");
    }

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

        var fuzzy = FindTypeCandidatesInDirectory(selectedLibDir, requestedTypeName);
        if (fuzzy.Count == 1)
        {
            var hit = fuzzy[0];
            return new PackageContentResult(selectedTfm, selectedLibDir, hit.AssemblyPath, hit.TypeName);
        }

        if (fuzzy.Count > 1)
        {
            throw BuildAmbiguousTypeException(requestedTypeName, selectedLibDir, fuzzy.Select(static x => x.TypeName));
        }

        return ResolveByMemberFallback(request.FullTypeName, requestedTypeName, selectedLibDir, selectedTfm);
    }

    private static InvalidOperationException BuildAmbiguousTypeException(string requestedTypeName, string location, IEnumerable<string> candidates)
    {
        var suggestions = string.Join(", ", candidates.Distinct(StringComparer.Ordinal).OrderBy(static x => x, StringComparer.Ordinal).Take(5));
        return new InvalidOperationException(
            $"Type '{requestedTypeName}' was ambiguous in '{location}'. Candidates: {suggestions}. Use --type with full namespace.");
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
                $"No type named '{requestedTypeName}' found in '{selectedLibDir}'. Found member '{memberName}' in multiple types: {suggestions}. Use --type with a fully-qualified type name. Try 'nupeek list --package <id>'.");
        }

        throw new InvalidOperationException($"No type named '{requestedTypeName}' found in '{selectedLibDir}'. Try 'nupeek list --package <id>' to inspect available types.");
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
            var hit = FindTypeInSingleAssembly(dll, fullTypeName);
            if (!string.IsNullOrWhiteSpace(hit))
            {
                return (dll, hit);
            }
        }

        return null;
    }

    private static List<(string AssemblyPath, string TypeName)> FindTypeCandidatesInDirectory(string libDir, string requestedTypeName)
        => Directory.GetFiles(libDir, "*.dll")
            .SelectMany(dll => FindTypeCandidatesInAssembly(dll, requestedTypeName)
                .Select(type => (AssemblyPath: dll, TypeName: type)))
            .DistinctBy(static x => (x.AssemblyPath, x.TypeName))
            .ToList();

    private static List<string> FindTypeCandidatesInAssembly(string assemblyPath, string requestedTypeName)
    {
        var requestedToken = requestedTypeName.Split('.').Last();

        return ReadTypeNamesFromAssembly(assemblyPath)
            .Where(type => IsTypeCandidate(type, requestedTypeName, requestedToken))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsTypeCandidate(string candidate, string requestedTypeName, string requestedToken)
    {
        if (string.Equals(candidate, requestedTypeName, StringComparison.Ordinal))
        {
            return true;
        }

        var typeName = candidate.Split('.').Last();
        return string.Equals(typeName, requestedToken, StringComparison.Ordinal)
               || typeName.StartsWith(requestedToken, StringComparison.OrdinalIgnoreCase)
               || candidate.StartsWith(requestedTypeName, StringComparison.OrdinalIgnoreCase);
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

    private static string? FindTypeInSingleAssembly(string assemblyPath, string fullTypeName)
        => ReadTypeNamesFromAssembly(assemblyPath)
            .FirstOrDefault(type => string.Equals(type, fullTypeName, StringComparison.Ordinal));

    private static List<string> FindDeclaringTypesForMemberNameInAssembly(string assemblyPath, string memberName)
    {
        var result = new List<string>();

        try
        {
            using var stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);
            if (!peReader.HasMetadata)
            {
                return result;
            }

            var md = peReader.GetMetadataReader();
            foreach (var handle in md.TypeDefinitions)
            {
                var typeDef = md.GetTypeDefinition(handle);
                if (HasMethod(typeDef, md, memberName)
                    || HasProperty(typeDef, md, memberName)
                    || HasField(typeDef, md, memberName)
                    || HasEvent(typeDef, md, memberName))
                {
                    result.Add(GetTypeFullName(md, handle));
                }
            }
        }
        catch
        {
            // fall through
        }

        return result.Distinct(StringComparer.Ordinal).ToList();
    }

    private static IEnumerable<string> ReadTypeNamesFromDirectory(string libDir)
        => Directory.GetFiles(libDir, "*.dll")
            .SelectMany(ReadTypeNamesFromAssembly)
            .Distinct(StringComparer.Ordinal);

    private static IEnumerable<string> ReadTypeNamesFromAssembly(string assemblyPath)
    {
        var result = new List<string>();

        try
        {
            using var stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);

            if (!peReader.HasMetadata)
            {
                return result;
            }

            var md = peReader.GetMetadataReader();
            foreach (var handle in md.TypeDefinitions)
            {
                result.Add(GetTypeFullName(md, handle));
            }
        }
        catch
        {
            // fall through
        }

        return result;
    }

    private static string GetTypeFullName(MetadataReader md, TypeDefinitionHandle handle)
    {
        var typeDef = md.GetTypeDefinition(handle);
        var ns = md.GetString(typeDef.Namespace);
        var name = md.GetString(typeDef.Name);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }
}
