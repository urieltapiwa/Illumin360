using Illumin360.Candidates.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>A labelled candidate count — a bucket (city or availability status) and its total.</summary>
/// <param name="Label">The bucket label.</param>
/// <param name="Count">Number of candidates in the bucket.</param>
public sealed record CountByLabel(string Label, int Count);

/// <summary>Aggregate statistics over the candidate pool, for dashboards and reporting.</summary>
/// <param name="Total">Total number of candidates.</param>
/// <param name="ByCity">Top cities by candidate count.</param>
/// <param name="ByAvailability">Candidate counts per availability status.</param>
public sealed record CandidateStatsDto(
    int Total,
    IReadOnlyList<CountByLabel> ByCity,
    IReadOnlyList<CountByLabel> ByAvailability);

/// <summary>Query: aggregate statistics over the candidate pool.</summary>
public sealed record GetCandidateStatsQuery : IQuery<CandidateStatsDto>;

/// <summary>Handles <see cref="GetCandidateStatsQuery"/> by reading aggregate counts from the repository.</summary>
public sealed class GetCandidateStatsQueryHandler(ICandidateRepository repository)
    : IQueryHandler<GetCandidateStatsQuery, CandidateStatsDto>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<CandidateStatsDto>> HandleAsync(
        GetCandidateStatsQuery query, CancellationToken cancellationToken)
    {
        var stats = await _repository.GetStatsAsync(cancellationToken).ConfigureAwait(false);
        return Result<CandidateStatsDto>.Success(stats);
    }
}
