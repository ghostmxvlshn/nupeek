namespace Nupeek.Cli;

internal static class InlineSourceReader
{
    public static InlineSourceResult ReadInlineSource(string outputPath, string emit, int maxChars)
    {
        if (!string.Equals(emit, "agent", StringComparison.Ordinal) || !File.Exists(outputPath))
        {
            return new InlineSourceResult(null, null, null, false);
        }

        var source = File.ReadAllText(outputPath);
        var originalChars = source.Length;
        var truncated = source.Length > maxChars;

        if (truncated)
        {
            source = source[..maxChars];
        }

        return new InlineSourceResult(source, maxChars, originalChars, truncated);
    }
}
