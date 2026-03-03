namespace Nupeek.Core;

public sealed record GraphType(string Name, string FullName, string? BaseType, IReadOnlyList<string> Interfaces);
