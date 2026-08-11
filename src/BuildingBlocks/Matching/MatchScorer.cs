namespace Illumin360.Matching;

/// <summary>A talent's matchable profile.</summary>
/// <param name="City">Home city.</param>
/// <param name="Role">Headline / current role.</param>
/// <param name="Skills">Skill names.</param>
/// <param name="SalaryExpectation">Expected pay, if stated (same currency as the role band).</param>
/// <param name="Seniority">Seniority level word (e.g. junior/mid/senior/lead), if known.</param>
public sealed record TalentProfile(string City, string Role, IReadOnlyCollection<string> Skills, int? SalaryExpectation = null, string? Seniority = null);

/// <summary>A role listing to score a talent against.</summary>
/// <param name="Title">Role title.</param>
/// <param name="City">Role city.</param>
/// <param name="Industry">Company industry.</param>
/// <param name="SalaryMin">Lower salary bound, if published.</param>
/// <param name="SalaryMax">Upper salary bound, if published.</param>
/// <param name="Seniority">Seniority level word for the role, if known.</param>
public sealed record RoleListing(string Title, string City, string Industry, int? SalaryMin = null, int? SalaryMax = null, string? Seniority = null);

/// <summary>One signal's contribution to a match score.</summary>
/// <param name="Name">Signal name (City / Role / Skills / Salary / Seniority).</param>
/// <param name="Weight">The signal's normalised weight (0–1, sums to 1 across signals).</param>
/// <param name="RawScore">The signal's raw fit (0–1).</param>
/// <param name="Points">Approximate points this signal adds to the 0–100 total.</param>
/// <param name="Reason">Human-readable explanation.</param>
public sealed record MatchSignal(string Name, double Weight, double RawScore, int Points, string Reason);

/// <summary>A match score with its per-signal breakdown ("why this match").</summary>
/// <param name="Score">The overall 0–100 score.</param>
/// <param name="Signals">The contributing signals, in evaluation order.</param>
public sealed record MatchExplanation(int Score, IReadOnlyList<MatchSignal> Signals);

/// <summary>
/// Deterministic talent↔role match scoring. Produces a 0–100 score from three weighted signals so the
/// whole platform ranks the same way:
/// <list type="bullet">
///   <item><description>City fit (0.35): same city as the role.</description></item>
///   <item><description>Role affinity (0.40): token overlap between the talent's role and the listing title.</description></item>
///   <item><description>Skill affinity (0.25): share of the talent's skills that appear in the listing title/industry.</description></item>
/// </list>
/// </summary>
public static class MatchScorer
{
    private const double CityWeight = 0.35;
    private const double RoleWeight = 0.40;
    private const double SkillWeight = 0.25;

    // Optional signals — only counted (and renormalised into the total) when both sides supply the data,
    // so callers passing only city/role/skills get exactly the same scores as before.
    private const double SalaryWeight = 0.20;
    private const double SeniorityWeight = 0.20;

    private static readonly char[] Separators =
        [' ', '\t', '\n', '\r', ',', '.', '/', '\\', '-', '_', '(', ')', '&', ':', ';', '|', '+'];

    // Very small stop-list so generic words don't inflate role affinity.
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "senior", "junior", "lead", "graduate", "intern", "trainee", "officer",
    };

    /// <summary>Scores a talent against a role listing (0–100).</summary>
    /// <param name="talent">The talent profile.</param>
    /// <param name="listing">The role listing.</param>
    /// <returns>A match score between 0 and 100.</returns>
    public static int Score(TalentProfile talent, RoleListing listing) => Explain(talent, listing).Score;

    /// <summary>
    /// Scores a talent against a role and returns a per-signal breakdown ("why this match") — each signal's
    /// weight, raw 0–1 score, its point contribution to the total, and a human-readable reason.
    /// </summary>
    /// <param name="talent">The talent profile.</param>
    /// <param name="listing">The role listing.</param>
    /// <returns>The score + signal contributions.</returns>
    public static MatchExplanation Explain(TalentProfile talent, RoleListing listing)
    {
        ArgumentNullException.ThrowIfNull(talent);
        ArgumentNullException.ThrowIfNull(listing);

        var sameCity = string.Equals(talent.City?.Trim(), listing.City?.Trim(), StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(talent.City);
        var cityScore = sameCity ? 1.0 : 0.0;

        var roleScore = Overlap(Tokenize(talent.Role), Tokenize(listing.Title));

        var listingText = Tokenize($"{listing.Title} {listing.Industry}");
        var skillTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in talent.Skills ?? [])
        {
            foreach (var token in Tokenize(skill))
            {
                skillTokens.Add(token);
            }
        }

        var matchedSkills = skillTokens.Count == 0 ? 0 : skillTokens.Count(listingText.Contains);
        var skillScore = skillTokens.Count == 0 ? 0.0 : (double)matchedSkills / skillTokens.Count;

        // Collect signal contributions; base three always count, salary/seniority only when data is present.
        var raw = new List<(string Name, double Weight, double Score, string Reason)>
        {
            ("City", CityWeight, cityScore, sameCity ? $"Same city ({listing.City})" : "Different city"),
            ("Role", RoleWeight, roleScore, $"Role-title overlap {Pct(roleScore)}"),
            ("Skills", SkillWeight, skillScore, skillTokens.Count == 0 ? "No skills listed" : $"Matched {matchedSkills} of {skillTokens.Count} skill terms"),
        };

        if (talent.SalaryExpectation is { } expectation && (listing.SalaryMin.HasValue || listing.SalaryMax.HasValue))
        {
            var s = SalarySignal(expectation, listing.SalaryMin, listing.SalaryMax);
            raw.Add(("Salary", SalaryWeight, s, s >= 1.0 ? "Expectation within budget" : "Expectation above the band"));
        }

        var talentRank = SeniorityParser.Rank(talent.Seniority);
        var listingRank = SeniorityParser.Rank(listing.Seniority);
        if (talentRank is { } tr && listingRank is { } lr)
        {
            var s = SenioritySignal(tr, lr);
            raw.Add(("Seniority", SeniorityWeight, s, s >= 1.0 ? "Seniority level matches" : s > 0 ? "Seniority one level off" : "Seniority mismatch"));
        }

        var weightSum = raw.Sum(x => x.Weight);
        var total = Math.Clamp((int)Math.Round(raw.Sum(x => x.Weight * x.Score) / weightSum * 100), 0, 100);

        var signals = raw
            .Select(x => new MatchSignal(x.Name, Math.Round(x.Weight / weightSum, 2), Math.Round(x.Score, 2), (int)Math.Round(x.Weight / weightSum * x.Score * 100), x.Reason))
            .ToList();

        return new MatchExplanation(total, signals);
    }

    private static string Pct(double fraction) => $"{(int)Math.Round(fraction * 100)}%";

    // 1.0 when the expectation fits within (or below) the role's band; decays above the ceiling.
    private static double SalarySignal(int expectation, int? min, int? max)
    {
        if (min is { } lo && expectation < lo)
        {
            return 1.0; // asking for less than the floor — comfortably affordable
        }

        if (max is { } hi)
        {
            if (expectation <= hi)
            {
                return 1.0;
            }

            var over = (double)(expectation - hi) / Math.Max(1, hi);
            return Math.Clamp(1.0 - over, 0.0, 1.0);
        }

        return 1.0; // no ceiling published — expectation is not a constraint
    }

    // 1.0 exact level, 0.5 one band off, 0.0 two or more bands off.
    private static double SenioritySignal(int talentRank, int listingRank)
    {
        var diff = Math.Abs(talentRank - listingRank);
        return diff switch
        {
            0 => 1.0,
            1 => 0.5,
            _ => 0.0,
        };
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

    // Share of the listing's title tokens that the talent's role also has (0–1).
    private static double Overlap(HashSet<string> talentTokens, HashSet<string> listingTokens)
    {
        if (listingTokens.Count == 0)
        {
            return 0.0;
        }

        var shared = listingTokens.Count(talentTokens.Contains);
        return (double)shared / listingTokens.Count;
    }
}
