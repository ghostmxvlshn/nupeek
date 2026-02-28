namespace Nupeek.Cli;

internal static class InlineSourceReader
{
    public static InlineSourceResult ReadInlineSource(string outputPath, string emit, int maxChars)
        => ReadInlineSourceAsync(outputPath, emit, maxChars).GetAwaiter().GetResult();

    public static async Task<InlineSourceResult> ReadInlineSourceAsync(string outputPath, string emit, int maxChars, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(emit, "agent", StringComparison.Ordinal) || !File.Exists(outputPath))
        {
            return new InlineSourceResult(null, null, null, false);
        }

        var source = await File.ReadAllTextAsync(outputPath, cancellationToken).ConfigureAwait(false);
        var originalChars = source.Length;
        var truncated = source.Length > maxChars;

        if (truncated)
        {
            source = source[..maxChars];
        }

        return new InlineSourceResult(source, maxChars, originalChars, truncated);
    }
}
