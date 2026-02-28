namespace Nupeek.Cli;

internal sealed record InlineSourceResult(
    string? Content,
    int? MaxChars,
    int? OriginalChars,
    bool Truncated);
