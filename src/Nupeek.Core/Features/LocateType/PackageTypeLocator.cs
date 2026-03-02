using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Nupeek.Core;

/// <summary>
/// Resolves TFM/lib directory and locates assembly containing a target type.
/// </summary>
public sealed class PackageTypeLocator
{
    private const int ScanConcurrency = 4;

    /// <summary>
    /// Lists all type names available in a local assembly.
    /// </summary>
    public async Task<IReadOnlyList<string>> ListTypesInAssemblyAsync(string assemblyPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);

        if (!File.Exists(assemblyPath))
        {
            throw new InvalidOperationException($"Assembly was not found: {assemblyPath}");
        }

        var types = await ReadTypeNamesFromAssemblyAsync(assemblyPath, cancellationToken).ConfigureAwait(false);
        return types
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Lists all type names available in a package lib directory for selected TFM.
    /// </summary>
    public async Task<IReadOnlyList<string>> ListTypesInPackageAsync(string extractedPath, string? tfm, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extractedPath);

        var selectedLibDir = SelectLibraryDirectory(extractedPath, tfm);
        var types = await ReadTypeNamesFromDirectoryAsync(selectedLibDir, cancellationToken).ConfigureAwait(false);

        return types
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Locates target type directly inside a specific assembly path.
    /// </summary>
    public async Task<PackageContentResult> LocateInAssemblyAsync(string assemblyPath, string fullTypeName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullTypeName);

        if (!File.Exists(assemblyPath))
        {
            throw new InvalidOperationException($"Assembly was not found: {assemblyPath}");
        }

        var normalizedType = TypeNameNormalizer.Normalize(fullTypeName);
        var dir = Path.GetDirectoryName(Path.GetFullPath(assemblyPath)) ?? ".";

        var exact = await FindTypeInSingleAssemblyAsync(assemblyPath, normalizedType, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(exact))
        {
            return new PackageContentResult("assembly", dir, assemblyPath, exact);
        }

        var fuzzy = await FindTypeCandidatesInAssemblyAsync(assemblyPath, normalizedType, cancellationToken).ConfigureAwait(false);
        if (fuzzy.Count == 1)
        {
            return new PackageContentResult("assembly", dir, assemblyPath, fuzzy[0]);
        }

        if (fuzzy.Count > 1)
        {
            throw BuildAmbiguousTypeException(normalizedType, assemblyPath, fuzzy);
        }

        var memberName = SymbolParser.ExtractMemberName(fullTypeName);
        var memberTypes = await FindDeclaringTypesForMemberNameInAssemblyAsync(assemblyPath, memberName, cancellationToken).ConfigureAwait(false);

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
    public async Task<PackageContentResult> LocateAsync(PackageContentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ExtractedPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FullTypeName);

        var requestedTypeName = TypeNameNormalizer.Normalize(request.FullTypeName);
        var selectedLibDir = SelectLibraryDirectory(request.ExtractedPath, request.Tfm);
        var selectedTfm = Path.GetFileName(selectedLibDir);

        var exact = await FindExactTypeAsync(selectedLibDir, requestedTypeName, cancellationToken).ConfigureAwait(false);
        if (exact is not null)
        {
            return new PackageContentResult(selectedTfm, selectedLibDir, exact.Value.AssemblyPath, exact.Value.TypeName);
        }

        var fuzzy = await FindTypeCandidatesInDirectoryAsync(selectedLibDir, requestedTypeName, cancellationToken).ConfigureAwait(false);
        if (fuzzy.Count == 1)
        {
            var hit = fuzzy[0];
            return new PackageContentResult(selectedTfm, selectedLibDir, hit.AssemblyPath, hit.TypeName);
        }

        if (fuzzy.Count > 1)
        {
            throw BuildAmbiguousTypeException(requestedTypeName, selectedLibDir, fuzzy.Select(static x => x.TypeName));
        }

        return await ResolveByMemberFallbackAsync(request.FullTypeName, requestedTypeName, selectedLibDir, selectedTfm, cancellationToken).ConfigureAwait(false);
    }

    private static InvalidOperationException BuildAmbiguousTypeException(string requestedTypeName, string location, IEnumerable<string> candidates)
    {
        var suggestions = string.Join(", ", candidates.Distinct(StringComparer.Ordinal).OrderBy(static x => x, StringComparer.Ordinal).Take(5));
        return new InvalidOperationException(
            $"Type '{requestedTypeName}' was ambiguous in '{location}'. Candidates: {suggestions}. Use --type with full namespace.");
    }

    private static async Task<PackageContentResult> ResolveByMemberFallbackAsync(string originalSymbol, string requestedTypeName, string selectedLibDir, string selectedTfm, CancellationToken cancellationToken)
    {
        var memberName = SymbolParser.ExtractMemberName(originalSymbol);
        var candidateTypes = await FindDeclaringTypesForMemberNameAsync(selectedLibDir, memberName, cancellationToken).ConfigureAwait(false);

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

    private static async Task<(string AssemblyPath, string TypeName)?> FindExactTypeAsync(string libDir, string fullTypeName, CancellationToken cancellationToken)
    {
        var hits = await RunBoundedAsync<string, (string dll, string hit)?>(
            Directory.GetFiles(libDir, "*.dll"),
            async dll =>
            {
                var hit = await FindTypeInSingleAssemblyAsync(dll, fullTypeName, cancellationToken).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(hit) ? null : (dll, hit);
            },
            cancellationToken).ConfigureAwait(false);

        return hits
            .Where(static x => x is not null)
            .Select(static x => x!.Value)
            .OrderBy(static x => x.dll, StringComparer.Ordinal)
            .Select(static x => (AssemblyPath: x.dll, TypeName: x.hit))
            .FirstOrDefault();
    }

    private static async Task<List<(string AssemblyPath, string TypeName)>> FindTypeCandidatesInDirectoryAsync(string libDir, string requestedTypeName, CancellationToken cancellationToken)
    {
        var all = await RunBoundedAsync(
            Directory.GetFiles(libDir, "*.dll"),
            async dll => (await FindTypeCandidatesInAssemblyAsync(dll, requestedTypeName, cancellationToken).ConfigureAwait(false))
                .Select(type => (AssemblyPath: dll, TypeName: type))
                .ToList(),
            cancellationToken).ConfigureAwait(false);

        return all.SelectMany(static x => x)
            .DistinctBy(static x => (x.AssemblyPath, x.TypeName))
            .ToList();
    }

    private static async Task<List<string>> FindTypeCandidatesInAssemblyAsync(string assemblyPath, string requestedTypeName, CancellationToken cancellationToken)
    {
        var requestedToken = requestedTypeName.Split('.').Last();
        var types = await ReadTypeNamesFromAssemblyAsync(assemblyPath, cancellationToken).ConfigureAwait(false);

        return types
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

    private static async Task<List<(string AssemblyPath, string TypeName)>> FindDeclaringTypesForMemberNameAsync(string libDir, string memberName, CancellationToken cancellationToken)
    {
        var all = await RunBoundedAsync(
            Directory.GetFiles(libDir, "*.dll"),
            dll => FindDeclaringTypesForMemberNameInAssemblyWithPathAsync(dll, memberName, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        return all.SelectMany(static x => x)
            .DistinctBy(static x => (x.AssemblyPath, x.TypeName))
            .ToList();
    }

    private static async Task<List<(string AssemblyPath, string TypeName)>> FindDeclaringTypesForMemberNameInAssemblyWithPathAsync(string assemblyPath, string memberName, CancellationToken cancellationToken)
    {
        var result = new List<(string AssemblyPath, string TypeName)>();
        var types = await ReadTypeMetadataAsync(assemblyPath, cancellationToken).ConfigureAwait(false);

        foreach (var type in types)
        {
            if (type.MemberNames.Contains(memberName, StringComparer.Ordinal))
            {
                result.Add((assemblyPath, type.FullTypeName));
            }
        }

        return result;
    }

    private static async Task<string?> FindTypeInSingleAssemblyAsync(string assemblyPath, string fullTypeName, CancellationToken cancellationToken)
        => (await ReadTypeNamesFromAssemblyAsync(assemblyPath, cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(type => string.Equals(type, fullTypeName, StringComparison.Ordinal));

    private static async Task<List<string>> FindDeclaringTypesForMemberNameInAssemblyAsync(string assemblyPath, string memberName, CancellationToken cancellationToken)
    {
        var types = await ReadTypeMetadataAsync(assemblyPath, cancellationToken).ConfigureAwait(false);
        return types
            .Where(x => x.MemberNames.Contains(memberName, StringComparer.Ordinal))
            .Select(x => x.FullTypeName)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static async Task<List<string>> ReadTypeNamesFromDirectoryAsync(string libDir, CancellationToken cancellationToken)
    {
        var all = await RunBoundedAsync(
            Directory.GetFiles(libDir, "*.dll"),
            dll => ReadTypeNamesFromAssemblyAsync(dll, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        return all.SelectMany(static x => x)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static async Task<List<string>> ReadTypeNamesFromAssemblyAsync(string assemblyPath, CancellationToken cancellationToken)
        => (await ReadTypeMetadataAsync(assemblyPath, cancellationToken).ConfigureAwait(false))
            .Select(static x => x.FullTypeName)
            .ToList();

    private static Task<List<TypeMetadata>> ReadTypeMetadataAsync(string assemblyPath, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            var result = new List<TypeMetadata>();

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                using var peReader = new PEReader(stream);

                if (!peReader.HasMetadata)
                {
                    return result;
                }

                var md = peReader.GetMetadataReader();
                foreach (var handle in md.TypeDefinitions)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var typeDef = md.GetTypeDefinition(handle);
                    var fullTypeName = GetTypeFullName(md, handle);
                    var members = CollectMemberNames(md, typeDef);

                    var baseType = ResolveEntityTypeName(md, typeDef.BaseType);
                    var interfaces = typeDef.GetInterfaceImplementations()
                        .Select(md.GetInterfaceImplementation)
                        .Select(x => ResolveEntityTypeName(md, x.Interface))
                        .Where(static x => !string.IsNullOrWhiteSpace(x))
                        .Cast<string>()
                        .Distinct(StringComparer.Ordinal)
                        .ToList();

                    result.Add(new TypeMetadata(fullTypeName, members, baseType, interfaces));
                }
            }
            catch
            {
                // swallow unreadable binaries for resilience
            }

            return result;
        }, cancellationToken);

    private static async Task<List<TResult>> RunBoundedAsync<TInput, TResult>(IEnumerable<TInput> items, Func<TInput, Task<TResult>> worker, CancellationToken cancellationToken)
    {
        using var gate = new SemaphoreSlim(ScanConcurrency);
        var tasks = items.Select(async item =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await worker(item).ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        });

        return (await Task.WhenAll(tasks).ConfigureAwait(false)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetRelatedTypesInAssemblyAsync(
        string assemblyPath,
        string rootTypeName,
        int depth,
        CancellationToken cancellationToken = default)
    {
        if (depth <= 0)
        {
            return [];
        }

        var normalizedRoot = TypeNameNormalizer.Normalize(rootTypeName);
        var types = await ReadTypeMetadataAsync(assemblyPath, cancellationToken).ConfigureAwait(false);
        var map = types
            .GroupBy(static x => x.FullTypeName, StringComparer.Ordinal)
            .ToDictionary(static g => g.Key, static g => g.First(), StringComparer.Ordinal);

        var visited = new HashSet<string>(StringComparer.Ordinal) { normalizedRoot };
        var frontier = new HashSet<string>(StringComparer.Ordinal) { normalizedRoot };

        for (var level = 0; level < depth; level++)
        {
            var next = new HashSet<string>(StringComparer.Ordinal);

            foreach (var current in frontier)
            {
                if (!map.TryGetValue(current, out var meta))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(meta.BaseType)
                    && !string.Equals(meta.BaseType, "System.Object", StringComparison.Ordinal)
                    && map.ContainsKey(meta.BaseType)
                    && visited.Add(meta.BaseType))
                {
                    next.Add(meta.BaseType);
                }

                foreach (var iface in meta.Interfaces)
                {
                    if (map.ContainsKey(iface) && visited.Add(iface))
                    {
                        next.Add(iface);
                    }
                }
            }

            frontier = next;
            if (frontier.Count == 0)
            {
                break;
            }
        }

        visited.Remove(normalizedRoot);
        return visited.OrderBy(static x => x, StringComparer.Ordinal).ToList();
    }

    private static HashSet<string> CollectMemberNames(MetadataReader md, TypeDefinition typeDef)
    {
        var members = new HashSet<string>(StringComparer.Ordinal);

        foreach (var method in typeDef.GetMethods().Select(md.GetMethodDefinition))
        {
            members.Add(md.GetString(method.Name));
        }

        foreach (var property in typeDef.GetProperties().Select(md.GetPropertyDefinition))
        {
            members.Add(md.GetString(property.Name));
        }

        foreach (var field in typeDef.GetFields().Select(md.GetFieldDefinition))
        {
            members.Add(md.GetString(field.Name));
        }

        foreach (var eventDef in typeDef.GetEvents().Select(md.GetEventDefinition))
        {
            members.Add(md.GetString(eventDef.Name));
        }

        return members;
    }

    private static string GetTypeFullName(MetadataReader md, TypeDefinitionHandle handle)
    {
        var typeDef = md.GetTypeDefinition(handle);
        var ns = md.GetString(typeDef.Namespace);
        var name = md.GetString(typeDef.Name);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    private static string? ResolveEntityTypeName(MetadataReader md, EntityHandle handle)
    {
        if (handle.IsNil)
        {
            return null;
        }

        return handle.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeFullName(md, (TypeDefinitionHandle)handle),
            HandleKind.TypeReference => GetTypeFullName(md, (TypeReferenceHandle)handle),
            HandleKind.TypeSpecification => null,
            _ => null,
        };
    }

    private static string GetTypeFullName(MetadataReader md, TypeReferenceHandle handle)
    {
        var typeRef = md.GetTypeReference(handle);
        var ns = md.GetString(typeRef.Namespace);
        var name = md.GetString(typeRef.Name);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    private sealed record TypeMetadata(string FullTypeName, HashSet<string> MemberNames, string? BaseType, IReadOnlyList<string> Interfaces);
}
