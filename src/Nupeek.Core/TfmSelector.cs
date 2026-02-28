namespace Nupeek.Core;

public static class TfmSelector
{
    private static readonly string[] Priority =
    [
        "net10.0", "net9.0", "net8.0", "net7.0", "net6.0", "netstandard2.1", "netstandard2.0"
    ];

    public static string SelectBest(IEnumerable<string> tfms)
    {
        var available = tfms?.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            ?? throw new ArgumentNullException(nameof(tfms));

        if (available.Count == 0)
        {
            throw new ArgumentException("At least one TFM must be provided", nameof(tfms));
        }

        foreach (var preferred in Priority)
        {
            var found = available.FirstOrDefault(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase));
            if (found is not null)
            {
                return found;
            }
        }

        return available.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).First();
    }
}
