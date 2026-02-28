namespace Nupeek.Core.Models;

public sealed record ManifestEntry(
    string PackageId,
    string Version,
    string Tfm,
    string TypeName,
    string AssemblyPath,
    string OutputPath,
    DateTimeOffset DecompiledAtUtc
);
