namespace Nupeek.Cli;

internal static class InputValidation
{
    public static string NormalizeFormat(string format)
    {
        if (string.Equals(format, "text", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            return format.ToLowerInvariant();
        }

        throw new ArgumentException("Invalid --format value. Allowed: text, json.", nameof(format));
    }

    public static string NormalizeProgress(string progress)
    {
        if (string.Equals(progress, "auto", StringComparison.OrdinalIgnoreCase)
            || string.Equals(progress, "always", StringComparison.OrdinalIgnoreCase)
            || string.Equals(progress, "never", StringComparison.OrdinalIgnoreCase))
        {
            return progress.ToLowerInvariant();
        }

        throw new ArgumentException("Invalid --progress value. Allowed: auto, always, never.", nameof(progress));
    }

    public static string NormalizeEmit(string emit)
    {
        if (string.Equals(emit, "files", StringComparison.OrdinalIgnoreCase)
            || string.Equals(emit, "agent", StringComparison.OrdinalIgnoreCase))
        {
            return emit.ToLowerInvariant();
        }

        throw new ArgumentException("Invalid --emit value. Allowed: files, agent.", nameof(emit));
    }

    public static int NormalizeMaxChars(int maxChars)
    {
        if (maxChars < 200)
        {
            throw new ArgumentException("Invalid --max-chars value. Minimum is 200.", nameof(maxChars));
        }

        return maxChars;
    }
}
