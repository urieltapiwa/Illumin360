using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Domain;
using Illumin360.Matching;
using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>
/// Finds candidates semantically closest to a seed candidate using embedding cosine similarity (v1 uses
/// the deterministic hashing provider — see 03-architecture/semantic-matching-design.md). Reuses
/// <see cref="SimilarCandidateDto"/> for its result shape.
/// </summary>
/// <param name="CandidateId">The seed candidate id.</param>
/// <param name="Take">How many matches to return.</param>
public sealed record GetSemanticSimilarCandidatesQuery(Guid CandidateId, int Take) : IQuery<IReadOnlyList<SimilarCandidateDto>>;

/// <summary>Handles <see cref="GetSemanticSimilarCandidatesQuery"/>.</summary>
/// <param name="repository">The candidate repository.</param>
/// <param name="embeddings">The embedding client (hashing by default; a hosted model when opted in).</param>
public sealed class GetSemanticSimilarCandidatesQueryHandler(ICandidateRepository repository, IEmbeddingClient embeddings)
    : IQueryHandler<GetSemanticSimilarCandidatesQuery, IReadOnlyList<SimilarCandidateDto>>
{
    private readonly ICandidateRepository _repository = repository;
    private readonly IEmbeddingClient _embeddings = embeddings;

    // The descriptive text embedded for a candidate (candidates carry no structured skills).
    private static string Text(Candidate c) => $"{c.PublicHeadline} {c.City}".Trim();

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SimilarCandidateDto>>> HandleAsync(GetSemanticSimilarCandidatesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var seed = await _repository.GetByIdAsync(new CandidateId(query.CandidateId), cancellationToken).ConfigureAwait(false);
        if (seed is null)
        {
            return Error.NotFound("candidate.not_found", "No matching candidate was found.");
        }

        var pool = await _repository.ListAllAsync(cancellationToken).ConfigureAwait(false);
        var take = Math.Clamp(query.Take <= 0 ? 5 : query.Take, 1, 20);

        var ranked = await SemanticRanker.RankAsync(
            _embeddings,
            Text(seed),
            pool.Select(c => (c.Id.Value, (string?)Text(c))),
            query.CandidateId,
            take,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var byId = pool.ToDictionary(c => c.Id.Value);
        return ranked
            .Where(m => byId.ContainsKey(m.Id))
            .Select(m =>
            {
                var c = byId[m.Id];
                return new SimilarCandidateDto(c.Id.Value, $"{c.FirstName} {c.LastName}", c.City, c.PublicHeadline, c.Availability.ToString(), m.Score);
            })
            .ToList();
    }
}
