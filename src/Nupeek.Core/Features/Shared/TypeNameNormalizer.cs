namespace Nupeek.Core;

/// <summary>
/// Normalizes user-friendly type names into CLR metadata naming where needed.
/// </summary>
public static class TypeNameNormalizer
{
    /// <summary>
    /// Converts C#-style generic type syntax (for example, <c>Type&lt;T&gt;</c>)
    /// into CLR arity syntax (for example, <c>Type`1</c>).
    /// </summary>
    public static string Normalize(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentException("Type name is required", nameof(typeName));
        }

        var clean = typeName.Trim();

        // Already CLR metadata form.
        if (clean.Contains('`', StringComparison.Ordinal))
        {
            return clean;
        }

        var lt = clean.IndexOf('<');
        var gt = clean.LastIndexOf('>');

        if (lt <= 0 || gt <= lt)
        {
            return clean;
        }

        var root = clean[..lt].Trim();
        var args = clean[(lt + 1)..gt].Trim();

        var arity = CountTopLevelGenericArgs(args);
        return $"{root}`{arity}";
    }

    private static int CountTopLevelGenericArgs(string args)
    {
        // Handles <> and <T> as arity 1.
        if (string.IsNullOrWhiteSpace(args))
        {
            return 1;
        }

        var depth = 0;
        var count = 1;

        foreach (var ch in args)
        {
            switch (ch)
            {
                case '<':
                    depth++;
                    break;
                case '>':
                    if (depth > 0)
                    {
                        depth--;
                    }
                    break;
                case ',' when depth == 0:
                    count++;
                    break;
            }
        }

        return Math.Max(1, count);
    }
}
