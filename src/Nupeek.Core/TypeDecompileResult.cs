namespace Nupeek.Core;

public sealed record TypeDecompileResult(
    string PackageId,
    string Version,
    string Tfm,
    string TypeName,
    string AssemblyPath,
    string OutputPath,
    string IndexPath,
    string ManifestPath);
