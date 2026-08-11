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
    /// <param name="useTaxonomy">
    /// When true, both sides are canonicalised through <see cref="SkillTaxonomy"/> before comparison, so
    /// synonyms match (e.g. "JS" ⇔ "JavaScript") and results read as canonical display names. Default false
    /// keeps the original case-insensitive raw-string behaviour.
    /// </param>
    /// <returns>The matched / missing / extra breakdown + coverage percentage.</returns>
    public static SkillGapResult Analyze(IEnumerable<string>? candidateSkills, IEnumerable<string>? requiredSkills, bool useTaxonomy = false)
    {
        var have = Project(candidateSkills, useTaxonomy);
        var required = Project(requiredSkills, useTaxonomy);
        var haveKeys = new HashSet<string>(have.Select(h => h.Key), StringComparer.Ordinal);
        var requiredKeys = new HashSet<string>(required.Select(r => r.Key), StringComparer.Ordinal);

        var matched = required.Where(r => haveKeys.Contains(r.Key)).Select(r => r.Display).ToList();
        var missing = required.Where(r => !haveKeys.Contains(r.Key)).Select(r => r.Display).ToList();
        var extra = have.Where(h => !requiredKeys.Contains(h.Key)).Select(h => h.Display).ToList();

        var coverage = required.Count == 0
            ? 100
            : (int)Math.Round((double)matched.Count / required.Count * 100);

        return new SkillGapResult(matched, missing, extra, coverage);
    }

    // De-duplicated by key, first-seen order preserved. Key drives comparison; Display is the human output.
    // Raw mode: key = display = trimmed-lowercased (original behaviour). Taxonomy mode: key = canonical id,
    // display = canonical display name.
    private static List<(string Key, string Display)> Project(IEnumerable<string>? skills, bool useTaxonomy)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<(string Key, string Display)>();
        foreach (var raw in skills ?? [])
        {
            string key;
            string display;
            if (useTaxonomy)
            {
                var canonical = SkillTaxonomy.Canonicalize(raw);
                key = canonical.Id;
                display = canonical.Display;
            }
            else
            {
                key = raw?.Trim().ToLowerInvariant() ?? string.Empty;
                display = key;
            }

            if (key.Length > 0 && seen.Add(key))
            {
                result.Add((key, display));
            }
        }

        return result;
    }
}
