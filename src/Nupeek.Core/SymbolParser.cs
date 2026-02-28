namespace Nupeek.Core;

/// <summary>
/// Converts symbol-like inputs (for example, <c>Namespace.Type.Method</c>) into type names.
/// </summary>
public static class SymbolParser
{
    /// <summary>
    /// Returns a type name derived from the provided symbol text.
    /// </summary>
    /// <remarks>
    /// If the input appears to include a member suffix, only the type part is returned.
    /// </remarks>
    public static string ToTypeName(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol is required", nameof(symbol));
        }

        // Normalize whitespace around user-provided input.
        var clean = symbol.Trim();

        // Heuristic: strip last segment as member name when dot-separated.
        var lastDot = clean.LastIndexOf('.');
        if (lastDot <= 0)
        {
            return clean;
        }

        return clean[..lastDot];
    }
}
