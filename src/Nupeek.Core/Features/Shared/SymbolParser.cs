namespace Nupeek.Core;

/// <summary>
/// Helpers for symbol-like user inputs.
/// </summary>
public static class SymbolParser
{
    /// <summary>
    /// Returns a lookup candidate for symbol text.
    /// </summary>
    /// <remarks>
    /// We preserve the input as much as possible to avoid incorrectly stripping
    /// fully-qualified type names that include multiple dotted namespace segments.
    /// </remarks>
    public static string ToTypeName(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol is required", nameof(symbol));
        }

        return symbol.Trim();
    }

    /// <summary>
    /// Extracts a member token from a symbol-like input.
    /// </summary>
    public static string ExtractMemberName(string symbol)
    {
        var clean = ToTypeName(symbol);

        var paren = clean.IndexOf('(');
        if (paren > 0)
        {
            clean = clean[..paren];
        }

        var lastDot = clean.LastIndexOf('.');
        if (lastDot < 0 || lastDot == clean.Length - 1)
        {
            return clean;
        }

        return clean[(lastDot + 1)..];
    }
}
