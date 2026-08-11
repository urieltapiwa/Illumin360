namespace Illumin360.Matching;

/// <summary>The attributes used to compare one candidate to another.</summary>
/// <param name="City">Home city.</param>
/// <param name="Availability">Availability status.</param>
/// <param name="Headline">Public headline / role text.</param>
public sealed record CandidateFeatures(string? City, string? Availability, string? Headline);

/// <summary>A similar candidate with its similarity score.</summary>
/// <param name="Id">Candidate id.</param>
/// <param name="Score">Similarity score (0–100).</param>
public sealed record SimilarMatch(Guid Id, int Score);

/// <summary>
/// Deterministic "more like this" ranking: scores every candidate in a pool against a seed candidate and
/// returns the closest matches. Similarity blends city (0.40), availability (0.20) and headline token
/// overlap (0.40). Pure and dependency-free — no vectors/ML.
/// </summary>
public static class CandidateSimilarity
{
    private const double CityWeight = 0.40;
    private const double AvailabilityWeight = 0.20;
    private const double HeadlineWeight = 0.40;

    private static readonly char[] Separators =
        [' ', '\t', '\n', '\r', ',', '.', '/', '\\', '-', '_', '(', ')', '&', ':', ';', '|', '+'];

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "senior", "junior", "lead", "graduate", "intern", "trainee", "officer",
    };

    /// <summary>Ranks a pool of candidates by similarity to a seed, returning the top matches.</summary>
    /// <param name="seed">The seed candidate's features.</param>
    /// <param name="pool">The candidate pool (id + features); the seed id, if present, is excluded.</param>
    /// <param name="seedId">The seed candidate's id (excluded from the results).</param>
    /// <param name="take">How many matches to return (clamped ≥ 0).</param>
    /// <returns>The closest candidates, most similar first (ties broken by id for determinism).</returns>
    public static IReadOnlyList<SimilarMatch> Rank(
        CandidateFeatures seed,
        IEnumerable<(Guid Id, CandidateFeatures Features)> pool,
        Guid seedId,
        int take)
    {
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(pool);
        if (take <= 0)
        {
            return [];
        }

        var seedTokens = Tokenize(seed.Headline);

        return pool
            .Where(p => p.Id != seedId)
            .Select(p => new SimilarMatch(p.Id, ScorePair(seed, seedTokens, p.Features)))
            .Where(m => m.Score > 0)
            .OrderByDescending(m => m.Score)
            .ThenBy(m => m.Id)
            .Take(take)
            .ToList();
    }

    private static int ScorePair(CandidateFeatures seed, HashSet<string> seedTokens, CandidateFeatures other)
    {
        var citySame = !string.IsNullOrWhiteSpace(seed.City)
            && string.Equals(seed.City?.Trim(), other.City?.Trim(), StringComparison.OrdinalIgnoreCase);
        var availSame = !string.IsNullOrWhiteSpace(seed.Availability)
            && string.Equals(seed.Availability?.Trim(), other.Availability?.Trim(), StringComparison.OrdinalIgnoreCase);

        var overlap = Jaccard(seedTokens, Tokenize(other.Headline));

        var weighted = (CityWeight * (citySame ? 1.0 : 0.0))
            + (AvailabilityWeight * (availSame ? 1.0 : 0.0))
            + (HeadlineWeight * overlap);
        return Math.Clamp((int)Math.Round(weighted * 100), 0, 100);
    }

    private static double Jaccard(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 || b.Count == 0)
        {
            return 0.0;
        }

        var intersection = a.Count(b.Contains);
        var union = a.Count + b.Count - intersection;
        return union == 0 ? 0.0 : (double)intersection / union;
    }

    private static HashSet<string> Tokenize(string? text)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text))
        {
            return set;
        }

        foreach (var raw in text.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (raw.Length >= 3 && !StopWords.Contains(raw))
            {
                set.Add(raw);
            }
        }

        return set;
    }
}
