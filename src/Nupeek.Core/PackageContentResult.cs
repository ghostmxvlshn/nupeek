namespace Nupeek.Core;

public sealed record PackageContentResult(
    string SelectedTfm,
    string LibDirectory,
    string AssemblyPath,
    string FullTypeName);
