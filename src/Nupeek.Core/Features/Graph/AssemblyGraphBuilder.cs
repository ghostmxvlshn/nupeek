using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Nupeek.Core;

public sealed class AssemblyGraphBuilder
{
    public async Task<AssemblyGraphResult> BuildAsync(string assemblyPath, string? rootType, int depth, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);

        if (!File.Exists(assemblyPath))
        {
            throw new InvalidOperationException($"Assembly was not found: {assemblyPath}");
        }

        var model = await ReadAssemblyAsync(assemblyPath, cancellationToken).ConfigureAwait(false);
        var selected = SelectTypes(model, rootType, depth);

        var types = model.Types.Where(t => selected.Contains(t.FullName)).OrderBy(t => t.FullName, StringComparer.Ordinal).ToList();
        var members = model.Members.Where(m => selected.Contains(m.DeclaringType)).OrderBy(m => m.DeclaringType, StringComparer.Ordinal).ThenBy(m => m.Name, StringComparer.Ordinal).ToList();
        var edges = model.Edges.Where(e => selected.Contains(e.FromType) && selected.Contains(e.ToType)).OrderBy(e => e.FromType, StringComparer.Ordinal).ThenBy(e => e.Relation, StringComparer.Ordinal).ThenBy(e => e.ToType, StringComparer.Ordinal).ToList();
        var globals = model.Globals.Where(g => selected.Contains(g.DeclaringType)).OrderBy(g => g.DeclaringType, StringComparer.Ordinal).ThenBy(g => g.Name, StringComparer.Ordinal).ToList();

        return new AssemblyGraphResult(types, members, edges, globals);
    }

    private static HashSet<string> SelectTypes(AssemblyGraphResult model, string? rootType, int depth)
    {
        if (string.IsNullOrWhiteSpace(rootType))
        {
            return model.Types.Select(t => t.FullName).ToHashSet(StringComparer.Ordinal);
        }

        var root = ResolveRootType(model, rootType);
        if (depth <= 0)
        {
            return [root];
        }

        var adjacency = model.Edges
            .GroupBy(e => e.FromType, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ToType).Distinct(StringComparer.Ordinal).ToList(), StringComparer.Ordinal);

        return TraverseDepth(root, depth, adjacency);
    }

    private static string ResolveRootType(AssemblyGraphResult model, string rootType)
    {
        var root = TypeNameNormalizer.Normalize(rootType);
        if (model.Types.Any(t => string.Equals(t.FullName, root, StringComparison.Ordinal)))
        {
            return root;
        }

        var token = root.Split('.').Last();
        var bySimple = model.Types
            .Where(t => string.Equals(t.Name, token, StringComparison.Ordinal) || t.Name.StartsWith(token, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.FullName)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (bySimple.Count == 1)
        {
            return bySimple[0];
        }

        throw new InvalidOperationException($"Root type '{root}' was not found in assembly graph.");
    }

    private static HashSet<string> TraverseDepth(string root, int depth, IReadOnlyDictionary<string, List<string>> adjacency)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal) { root };
        var frontier = new HashSet<string>(StringComparer.Ordinal) { root };

        for (var d = 0; d < depth; d++)
        {
            var next = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in frontier)
            {
                if (!adjacency.TryGetValue(node, out var neighbors))
                {
                    continue;
                }

                foreach (var n in neighbors.Where(visited.Add))
                {
                    next.Add(n);
                }
            }

            if (next.Count == 0)
            {
                break;
            }

            frontier = next;
        }

        return visited;
    }

    private static Task<AssemblyGraphResult> ReadAssemblyAsync(string assemblyPath, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            var types = new List<GraphType>();
            var members = new List<GraphMember>();
            var edges = new List<GraphEdge>();
            var globals = new List<GraphGlobal>();

            using var stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);
            if (!peReader.HasMetadata)
            {
                return new AssemblyGraphResult(types, members, edges, globals);
            }

            var md = peReader.GetMetadataReader();
            foreach (var handle in md.TypeDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessType(md, handle, types, members, edges, globals);
            }

            return new AssemblyGraphResult(types, members, edges, globals);
        }, cancellationToken);

    private static void ProcessType(
        MetadataReader md,
        TypeDefinitionHandle handle,
        ICollection<GraphType> types,
        ICollection<GraphMember> members,
        ICollection<GraphEdge> edges,
        ICollection<GraphGlobal> globals)
    {
        var td = md.GetTypeDefinition(handle);
        var typeName = GetTypeFullName(md, handle);
        var baseType = ResolveEntityTypeName(md, td.BaseType);
        var interfaces = td.GetInterfaceImplementations()
            .Select(md.GetInterfaceImplementation)
            .Select(i => ResolveEntityTypeName(md, i.Interface))
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        types.Add(new GraphType(typeName.Split('.').Last(), typeName, baseType, interfaces));
        AddRelationEdges(typeName, baseType, interfaces, edges);
        AddMembers(md, td, typeName, members, globals);
    }

    private static void AddRelationEdges(string typeName, string? baseType, IEnumerable<string> interfaces, ICollection<GraphEdge> edges)
    {
        if (!string.IsNullOrWhiteSpace(baseType))
        {
            edges.Add(new GraphEdge(typeName, "inherits", baseType!));
        }

        foreach (var iface in interfaces)
        {
            edges.Add(new GraphEdge(typeName, "implements", iface));
        }
    }

    private static void AddMembers(MetadataReader md, TypeDefinition td, string typeName, ICollection<GraphMember> members, ICollection<GraphGlobal> globals)
    {
        foreach (var mh in td.GetMethods())
        {
            var m = md.GetMethodDefinition(mh);
            members.Add(new GraphMember(typeName, "method", md.GetString(m.Name), IsStatic(m.Attributes), GetVisibility(m.Attributes)));
        }

        foreach (var ph in td.GetProperties())
        {
            var p = md.GetPropertyDefinition(ph);
            members.Add(new GraphMember(typeName, "property", md.GetString(p.Name), false, "unknown"));
        }

        foreach (var fh in td.GetFields())
        {
            var f = md.GetFieldDefinition(fh);
            var name = md.GetString(f.Name);
            var isStatic = IsStatic(f.Attributes);
            var visibility = GetVisibility(f.Attributes);
            members.Add(new GraphMember(typeName, "field", name, isStatic, visibility));

            if (isStatic)
            {
                globals.Add(new GraphGlobal(typeName, name, visibility, f.Attributes.HasFlag(FieldAttributes.Literal)));
            }
        }

        foreach (var eh in td.GetEvents())
        {
            var e = md.GetEventDefinition(eh);
            members.Add(new GraphMember(typeName, "event", md.GetString(e.Name), false, "unknown"));
        }
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
            _ => null,
        };
    }

    private static string GetTypeFullName(MetadataReader md, TypeReferenceHandle handle)
    {
        var tr = md.GetTypeReference(handle);
        var ns = md.GetString(tr.Namespace);
        var name = md.GetString(tr.Name);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    private static bool IsStatic(MethodAttributes attributes) => attributes.HasFlag(MethodAttributes.Static);

    private static bool IsStatic(FieldAttributes attributes) => attributes.HasFlag(FieldAttributes.Static);

    private static string GetVisibility(MethodAttributes attributes)
        => attributes switch
        {
            _ when attributes.HasFlag(MethodAttributes.Public) => "public",
            _ when attributes.HasFlag(MethodAttributes.Private) => "private",
            _ when attributes.HasFlag(MethodAttributes.Family) => "protected",
            _ when attributes.HasFlag(MethodAttributes.Assembly) => "internal",
            _ => "unknown",
        };

    private static string GetVisibility(FieldAttributes attributes)
        => attributes switch
        {
            _ when attributes.HasFlag(FieldAttributes.Public) => "public",
            _ when attributes.HasFlag(FieldAttributes.Private) => "private",
            _ when attributes.HasFlag(FieldAttributes.Family) => "protected",
            _ when attributes.HasFlag(FieldAttributes.Assembly) => "internal",
            _ => "unknown",
        };
}
