namespace Nupeek.Core;

/// <summary>
/// Input contract for end-to-end decompilation of a single type.
/// </summary>
/// <param name="PackageId">NuGet package id.</param>
/// <param name="Version">Optional package version. If null, latest stable is used.</param>
/// <param name="Tfm">Optional target framework. If null, best available is selected.</param>
/// <param name="TypeName">Fully-qualified target type name to decompile.</param>
/// <param name="OutputRoot">Output root for generated source and catalogs.</param>
public sealed record TypeDecompileRequest(
    string PackageId,
    string? Version,
    string? Tfm,
    string TypeName,
    string OutputRoot);
