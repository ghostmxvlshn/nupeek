namespace Nupeek.Core;

/// <summary>
/// Input for resolving package content location for a target type.
/// </summary>
/// <param name="ExtractedPath">Root path of extracted package content.</param>
/// <param name="FullTypeName">Fully-qualified type name to locate.</param>
/// <param name="Tfm">Optional explicit TFM. If null, selector chooses best available.</param>
public sealed record PackageContentRequest(
    string ExtractedPath,
    string FullTypeName,
    string? Tfm);
