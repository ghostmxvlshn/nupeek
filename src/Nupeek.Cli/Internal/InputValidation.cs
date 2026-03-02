namespace Nupeek.Cli;

internal static class InputValidation
{
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
}
