namespace Illumin360.Matching;

/// <summary>A rediscovery fit score (0–100) plus a short human reason.</summary>
/// <param name="Value">The blended rediscovery score, 0–100.</param>
/// <param name="Reason">A short human-readable explanation.</param>
public sealed record RediscoveryScore(int Value, string Reason);

/// <summary>
/// Pure, dependency-free ranker for "silver-medalist" rediscovery: given a target role, scores a talent's
/// PAST (not-hired) application to some other role by how well that prior engagement predicts fit for the
/// target — title similarity + same city + how strong the prior match was + how far they advanced.
/// No external data or cross-service calls; Recruitment owns every input.
/// </summary>
public static class RediscoveryScorer
{
    private const double TitleWeight = 0.45;
    private const double CityWeight = 0.15;
    private const double PriorMatchWeight = 0.20;
    private const double AdvancementWeight = 0.20;

    /// <summary>Scores a prior not-hired application against a target role.</summary>
    /// <param name="targetTitle">The target requisition's title.</param>
    /// <param name="targetCity">The target requisition's city.</param>
    /// <param name="priorTitle">The title of the role the talent previously applied to.</param>
    /// <param name="priorCity">The city of that prior role.</param>
    /// <param name="priorMatchScore">The match score (0–100) recorded on the prior application.</param>
    /// <param name="interviewCount">How many interviews the prior application reached.</param>
    /// <param name="hadOffer">Whether the prior application received an offer (declined/withdrawn ⇒ strong signal).</param>
    /// <returns>A blended 0–100 score with a reason.</returns>
    public static RediscoveryScore Evaluate(
        string targetTitle,
        string targetCity,
        string priorTitle,
        string priorCity,
        decimal priorMatchScore,
        int interviewCount,
        bool hadOffer)
    {
        var titleSim = TitleSimilarity(targetTitle, priorTitle);
        var cityMatch = string.Equals((targetCity ?? string.Empty).Trim(), (priorCity ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(targetCity) ? 1.0 : 0.0;
        var priorMatch = Math.Clamp((double)priorMatchScore / 100.0, 0.0, 1.0);
        var advancement = hadOffer ? 1.0 : interviewCount > 0 ? 0.6 : 0.25;

        var blended = (titleSim * TitleWeight) + (cityMatch * CityWeight) + (priorMatch * PriorMatchWeight) + (advancement * AdvancementWeight);
        var value = (int)Math.Round(Math.Clamp(blended, 0.0, 1.0) * 100);

        var reason = BuildReason(hadOffer, interviewCount, cityMatch > 0, titleSim);
        return new RediscoveryScore(value, reason);
    }

    // Token-set Jaccard over the two titles (lowercased, split on non-alphanumerics).
    private static double TitleSimilarity(string a, string b)
    {
        var setA = Tokens(a);
        var setB = Tokens(b);
        if (setA.Count == 0 || setB.Count == 0)
        {
            return 0.0;
        }

        var intersection = setA.Intersect(setB).Count();
        var union = setA.Union(setB).Count();
        return union == 0 ? 0.0 : (double)intersection / union;
    }

    private static HashSet<string> Tokens(string? text)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text))
        {
            return set;
        }

        foreach (var token in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var cleaned = new string(token.Where(char.IsLetterOrDigit).ToArray());
            if (cleaned.Length > 1)
            {
                set.Add(cleaned);
            }
        }

        return set;
    }

    private static string BuildReason(bool hadOffer, int interviewCount, bool sameCity, double titleSim)
    {
        var parts = new List<string>();
        if (hadOffer)
        {
            parts.Add("reached an offer previously");
        }
        else if (interviewCount > 0)
        {
            parts.Add($"interviewed {interviewCount}× for a similar role");
        }

        if (titleSim >= 0.5)
        {
            parts.Add("very similar prior role");
        }
        else if (titleSim > 0)
        {
            parts.Add("related prior role");
        }

        if (sameCity)
        {
            parts.Add("same city");
        }

        return parts.Count == 0 ? "Applied before but wasn't hired" : string.Join(", ", parts);
    }
}
