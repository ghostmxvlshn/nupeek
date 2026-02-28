namespace Nupeek.Core;

public sealed record TypeDecompileRequest(
    string PackageId,
    string? Version,
    string? Tfm,
    string TypeName,
    string OutputRoot);
