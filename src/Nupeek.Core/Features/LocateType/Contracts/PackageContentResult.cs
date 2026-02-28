namespace Nupeek.Core;

/// <summary>
/// Resolved package content details for a specific type.
/// </summary>
/// <param name="SelectedTfm">TFM selected for lookup/decompilation.</param>
/// <param name="LibDirectory">Selected <c>lib/&lt;tfm&gt;</c> directory.</param>
/// <param name="AssemblyPath">Assembly path containing target type.</param>
/// <param name="FullTypeName">Resolved fully-qualified type name.</param>
public sealed record PackageContentResult(
    string SelectedTfm,
    string LibDirectory,
    string AssemblyPath,
    string FullTypeName);
