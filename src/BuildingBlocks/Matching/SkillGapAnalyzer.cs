namespace Illumin360.Matching;

/// <summary>The outcome of a skill-gap analysis of a candidate against a role's required skills.</summary>
/// <param name="Matched">Required skills the candidate already has (normalised, in required order).</param>
/// <param name="Missing">Required skills the candidate lacks — the upskilling target.</param>
/// <param name="Extra">Candidate skills not asked for by the role.</param>
/// <param name="CoveragePercent">Share of required skills the candidate has (0–100).</param>
public sealed record SkillGapResult(
    IReadOnlyList<string> Matched,
    IReadOnlyList<string> Missing,
    IReadOnlyList<string> Extra,
    int CoveragePercent);

/// <summary>
/// Deterministic skill-gap analysis: compares a candidate's skills against a role's required skills and
/// reports what's matched, what's missing (the upskilling target) and what's extra. Case-insensitive and
/// whitespace-tolerant; a role with no required skills is treated as 100% covered.
/// </summary>
public static class SkillGapAnalyzer
{
    /// <summary>Analyses a candidate's skills against a role's required skills.</summary>
    /// <param name="candidateSkills">The candidate's skills.</param>
    /// <param name="requiredSkills">The role's required skills.</param>
    /// <returns>The matched / missing / extra breakdown + coverage percentage.</returns>
    public static SkillGapResult Analyze(IEnumerable<string>? candidateSkills, IEnumerable<string>? requiredSkills)
    {
        var have = Normalize(candidateSkills);
        var required = NormalizeOrdered(requiredSkills);
        var haveSet = new HashSet<string>(have, StringComparer.Ordinal);
        var requiredSet = new HashSet<string>(required, StringComparer.Ordinal);

        var matched = required.Where(haveSet.Contains).ToList();
        var missing = required.Where(r => !haveSet.Contains(r)).ToList();
        var extra = have.Where(h => !requiredSet.Contains(h)).ToList();

        var coverage = required.Count == 0
            ? 100
            : (int)Math.Round((double)matched.Count / required.Count * 100);

        return new SkillGapResult(matched, missing, extra, coverage);
    }

    // Normalised, de-duplicated, order-not-preserved (for the candidate side).
    private static List<string> Normalize(IEnumerable<string>? skills)
        => (skills ?? [])
            .Select(s => s?.Trim().ToLowerInvariant() ?? string.Empty)
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    // Normalised, de-duplicated, first-seen order preserved (for the required side).
    private static List<string> NormalizeOrdered(IEnumerable<string>? skills)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var raw in skills ?? [])
        {
            var s = raw?.Trim().ToLowerInvariant() ?? string.Empty;
            if (s.Length > 0 && seen.Add(s))
            {
                result.Add(s);
            }
        }

        return result;
    }
}
