namespace Nupeek.Core;

public sealed record GraphMember(string DeclaringType, string Kind, string Name, bool IsStatic, string Visibility);
