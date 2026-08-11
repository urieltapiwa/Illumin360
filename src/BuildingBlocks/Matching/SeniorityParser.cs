namespace Illumin360.Matching;

/// <summary>
/// Maps free-text seniority wording (from a role title or a talent headline) onto an ordinal rank so
/// levels can be compared. Deterministic; scans tokens for the first recognised level keyword.
/// </summary>
public static class SeniorityParser
{
    // Ordinal ladder — higher = more senior. Word variants map to the same rank.
    private static readonly (string Word, int Rank)[] Levels =
    [
        ("intern", 0), ("internship", 0), ("trainee", 0), ("graduate", 0), ("entry", 0),
        ("junior", 1), ("jnr", 1), ("associate", 1),
        ("mid", 2), ("intermediate", 2), ("mid-level", 2),
        ("senior", 3), ("snr", 3), ("sr", 3),
        ("lead", 4), ("principal", 4), ("staff", 4), ("head", 4), ("manager", 4),
    ];

    private static readonly char[] Separators =
        [' ', '\t', '\n', '\r', ',', '.', '/', '\\', '-', '_', '(', ')', '&', ':', ';', '|', '+'];

    /// <summary>Resolves a seniority rank (0 = entry … 4 = lead+) from text, or null if none recognised.</summary>
    /// <param name="text">The role title / headline / explicit level word.</param>
    /// <returns>The ordinal rank, or null.</returns>
    public static int? Rank(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        foreach (var token in text.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var (word, rank) in Levels)
            {
                if (string.Equals(token, word, StringComparison.OrdinalIgnoreCase))
                {
                    return rank;
                }
            }
        }

        return null;
    }
}
