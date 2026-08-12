namespace Illumin360.Matching;

/// <summary>A semantically-ranked match with its similarity score.</summary>
/// <param name="Id">The item id.</param>
/// <param name="Score">Cosine-derived similarity (0–100).</param>
public sealed record SemanticMatch(Guid Id, int Score);

/// <summary>
/// Ranks a pool of text-bearing items by embedding cosine similarity to a query. Compute-on-query using the
/// supplied <see cref="IEmbeddingProvider"/> — for the hashing provider this is cheap and needs no stored
/// vectors; a persisted (pgvector) path is only needed once an expensive hosted model is enabled.
/// </summary>
public static class SemanticRanker
{
    /// <summary>Ranks the pool against the query text, most similar first.</summary>
    /// <param name="provider">The embedding provider.</param>
    /// <param name="queryText">The seed text (e.g. a candidate's or role's descriptive text).</param>
    /// <param name="pool">The items to rank (id + text).</param>
    /// <param name="excludeId">An id to exclude from the results (e.g. the seed itself).</param>
    /// <param name="take">Maximum results to return.</param>
    /// <param name="minScore">Minimum score (0–100) to include (default 1 — drops non-matches).</param>
    /// <returns>The closest items, most similar first (ties broken by id).</returns>
    public static IReadOnlyList<SemanticMatch> Rank(
        IEmbeddingProvider provider,
        string? queryText,
        IEnumerable<(Guid Id, string? Text)> pool,
        Guid excludeId,
        int take,
        int minScore = 1)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(pool);
        if (take <= 0 || string.IsNullOrWhiteSpace(queryText))
        {
            return [];
        }

        var query = provider.Embed(queryText);

        return pool
            .Where(p => p.Id != excludeId)
            .Select(p => new SemanticMatch(p.Id, ToScore(VectorMath.Cosine(query, provider.Embed(p.Text)))))
            .Where(m => m.Score >= minScore)
            .OrderByDescending(m => m.Score)
            .ThenBy(m => m.Id)
            .Take(take)
            .ToList();
    }

    /// <summary>
    /// Async variant using an <see cref="IEmbeddingClient"/> — the path for a hosted model. Embeds the query
    /// then each pool item (compute-on-query; a persisted pgvector path is the scale follow-up once a hosted
    /// model is enabled). Most similar first, ties broken by id.
    /// </summary>
    /// <param name="client">The embedding client.</param>
    /// <param name="queryText">The seed text.</param>
    /// <param name="pool">The items to rank (id + text).</param>
    /// <param name="excludeId">An id to exclude from the results.</param>
    /// <param name="take">Maximum results to return.</param>
    /// <param name="minScore">Minimum score (0–100) to include (default 1).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The closest items, most similar first.</returns>
    public static async Task<IReadOnlyList<SemanticMatch>> RankAsync(
        IEmbeddingClient client,
        string? queryText,
        IEnumerable<(Guid Id, string? Text)> pool,
        Guid excludeId,
        int take,
        int minScore = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(pool);
        if (take <= 0 || string.IsNullOrWhiteSpace(queryText))
        {
            return [];
        }

        var query = await client.EmbedAsync(queryText, cancellationToken).ConfigureAwait(false);

        var scored = new List<SemanticMatch>();
        foreach (var (id, text) in pool)
        {
            if (id == excludeId)
            {
                continue;
            }

            var vector = await client.EmbedAsync(text, cancellationToken).ConfigureAwait(false);
            var score = ToScore(VectorMath.Cosine(query, vector));
            if (score >= minScore)
            {
                scored.Add(new SemanticMatch(id, score));
            }
        }

        return scored.OrderByDescending(m => m.Score).ThenBy(m => m.Id).Take(take).ToList();
    }

    // Cosine in [-1,1] → a 0–100 score (negatives clamp to 0).
    private static int ToScore(double cosine) => (int)Math.Round(Math.Clamp(cosine, 0, 1) * 100);
}
