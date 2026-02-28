namespace Nupeek.Core;

/// <summary>
/// Selects the most suitable TFM from available package targets.
/// </summary>
public static class TfmSelector
{
    // Priority order tuned for current Nupeek runtime expectations.
    private static readonly string[] Priority =
    [
        "net10.0", "net9.0", "net8.0", "net7.0", "net6.0", "netstandard2.1", "netstandard2.0"
    ];

    /// <summary>
    /// Picks the best TFM from the candidate set.
    /// </summary>
    public static string SelectBest(IEnumerable<string> tfms)
    {
        var available = tfms?.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            ?? throw new ArgumentNullException(nameof(tfms));

        if (available.Count == 0)
        {
            throw new ArgumentException("At least one TFM must be provided", nameof(tfms));
        }

        // First pass: preferred known TFMs.
        foreach (var preferred in Priority)
        {
            var found = available.FirstOrDefault(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase));
            if (found is not null)
            {
                return found;
            }
        }

        // Fallback keeps deterministic behavior even for unknown/custom TFMs.
        return available.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).First();
    }
}
