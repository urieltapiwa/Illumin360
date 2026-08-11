using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Domain;
using Illumin360.Matching;
using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>A candidate similar to a seed candidate ("more like this").</summary>
/// <param name="Id">Candidate id.</param>
/// <param name="Name">Full name.</param>
/// <param name="City">City.</param>
/// <param name="Headline">Public headline.</param>
/// <param name="Availability">Availability status.</param>
/// <param name="Score">Similarity score (0–100).</param>
public sealed record SimilarCandidateDto(Guid Id, string Name, string City, string? Headline, string Availability, int Score);

/// <summary>Finds candidates most similar to a seed candidate.</summary>
/// <param name="CandidateId">The seed candidate id.</param>
/// <param name="Take">How many similar candidates to return.</param>
public sealed record GetSimilarCandidatesQuery(Guid CandidateId, int Take) : IQuery<IReadOnlyList<SimilarCandidateDto>>;

/// <summary>Handles <see cref="GetSimilarCandidatesQuery"/> using the shared <see cref="CandidateSimilarity"/> ranker.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class GetSimilarCandidatesQueryHandler(ICandidateRepository repository)
    : IQueryHandler<GetSimilarCandidatesQuery, IReadOnlyList<SimilarCandidateDto>>
{
    private readonly ICandidateRepository _repository = repository;

    private static CandidateFeatures Features(Candidate c) =>
        new(c.City, c.Availability.ToString(), c.PublicHeadline);

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SimilarCandidateDto>>> HandleAsync(GetSimilarCandidatesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var seed = await _repository.GetByIdAsync(new CandidateId(query.CandidateId), cancellationToken).ConfigureAwait(false);
        if (seed is null)
        {
            return Error.NotFound("candidate.not_found", "No matching candidate was found.");
        }

        var pool = await _repository.ListAllAsync(cancellationToken).ConfigureAwait(false);
        var take = Math.Clamp(query.Take <= 0 ? 5 : query.Take, 1, 20);

        var ranked = CandidateSimilarity.Rank(
            Features(seed),
            pool.Select(c => (c.Id.Value, Features(c))),
            query.CandidateId,
            take);

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
