namespace Nupeek.Core;

public sealed record PackageContentRequest(
    string ExtractedPath,
    string FullTypeName,
    string? Tfm);
