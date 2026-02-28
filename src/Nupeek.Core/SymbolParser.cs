namespace Nupeek.Core;

public static class SymbolParser
{
    public static string ToTypeName(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol is required", nameof(symbol));

        var clean = symbol.Trim();
        var lastDot = clean.LastIndexOf('.');
        if (lastDot <= 0)
            return clean;

        return clean[..lastDot];
    }
}
