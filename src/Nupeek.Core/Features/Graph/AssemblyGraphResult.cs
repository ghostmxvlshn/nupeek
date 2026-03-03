namespace Nupeek.Core;

public sealed record AssemblyGraphResult(
    IReadOnlyList<GraphType> Types,
    IReadOnlyList<GraphMember> Members,
    IReadOnlyList<GraphEdge> Edges,
    IReadOnlyList<GraphGlobal> Globals);
