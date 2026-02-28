namespace Nupeek.Core;

/// <summary>
/// Output contract for a successful type decompilation run.
/// </summary>
/// <param name="PackageId">Resolved package id.</param>
/// <param name="Version">Resolved package version.</param>
/// <param name="Tfm">Resolved target framework used for lookup.</param>
/// <param name="TypeName">Target type that was decompiled.</param>
/// <param name="AssemblyPath">Assembly path used for decompilation.</param>
/// <param name="OutputPath">Generated decompiled source file path.</param>
/// <param name="IndexPath">Updated type index file path.</param>
/// <param name="ManifestPath">Updated manifest file path.</param>
public sealed record TypeDecompileResult(
    string PackageId,
    string Version,
    string Tfm,
    string TypeName,
    string AssemblyPath,
    string OutputPath,
    string IndexPath,
    string ManifestPath);
