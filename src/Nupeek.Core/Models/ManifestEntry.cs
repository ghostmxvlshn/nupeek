namespace Nupeek.Core.Models;

/// <summary>
/// Provenance entry for one generated decompiled type.
/// </summary>
/// <param name="PackageId">Package id source.</param>
/// <param name="Version">Package version source.</param>
/// <param name="Tfm">TFM used for decompilation.</param>
/// <param name="TypeName">Fully-qualified type name.</param>
/// <param name="AssemblyPath">Assembly path where type was found.</param>
/// <param name="OutputPath">Generated output file path.</param>
/// <param name="DecompiledAtUtc">UTC timestamp when output was generated.</param>
public sealed record ManifestEntry(
    string PackageId,
    string Version,
    string Tfm,
    string TypeName,
    string AssemblyPath,
    string OutputPath,
    DateTimeOffset DecompiledAtUtc
);
