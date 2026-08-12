namespace Illumin360.Matching;

/// <summary>A rolled-up reputation snapshot for a talent (or client).</summary>
/// <param name="Score">Reputation score, 0–100 (Bayesian-shrunk so a single rating can't be a perfect 100).</param>
/// <param name="Count">Number of ratings behind the score.</param>
/// <param name="Average">Raw mean rating (1–5), or 0 when there are no ratings.</param>
public sealed record ReputationSnapshot(int Score, int Count, double Average);

/// <summary>
/// Pure, deterministic reputation scorer — an Upwork-JSS-style trust signal. Blends 1–5 ratings into a 0–100
/// score using Bayesian shrinkage toward a neutral prior, so a lone 5★ doesn't outrank a long track record and
/// a lone 1★ isn't fatal. Dependency-free and unit-testable; lives in the matching library so it can feed
/// ranking (a reputation term in explanations / the learned ranker's feature vector).
/// </summary>
public static class ReputationScorer
{
    // Neutral prior (3.5 of 5) with a confidence weight of PriorWeight "virtual" ratings. New talents start near
    // the prior; the observed mean takes over as real ratings accumulate.
    private const double PriorMean = 3.5;
    private const double PriorWeight = 3.0;

    /// <summary>Computes a reputation snapshot from a set of 1–5 ratings.</summary>
    /// <param name="ratings">The ratings (values outside 1–5 are clamped).</param>
    /// <returns>The 0–100 score, count, and raw average.</returns>
    public static ReputationSnapshot Score(IEnumerable<int>? ratings)
    {
        var values = (ratings ?? []).Select(r => Math.Clamp(r, 1, 5)).ToList();
        if (values.Count == 0)
        {
            return new ReputationSnapshot(0, 0, 0);
        }

        var sum = values.Sum();
        var average = (double)sum / values.Count;

        // Shrink the mean toward the prior, then map 1–5 → 0–100.
        var shrunk = ((PriorMean * PriorWeight) + sum) / (PriorWeight + values.Count);
        var score = (int)Math.Round((shrunk - 1.0) / 4.0 * 100.0);

        return new ReputationSnapshot(Math.Clamp(score, 0, 100), values.Count, Math.Round(average, 2));
    }
}
