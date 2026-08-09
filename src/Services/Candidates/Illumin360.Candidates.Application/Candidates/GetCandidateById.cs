using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>Query: fetch a single candidate by id.</summary>
/// <param name="Id">The candidate id.</param>
public sealed record GetCandidateByIdQuery(Guid Id) : IQuery<CandidateDto>;

/// <summary>Handles <see cref="GetCandidateByIdQuery"/> by reading from the candidate repository.</summary>
public sealed class GetCandidateByIdQueryHandler(ICandidateRepository repository)
    : IQueryHandler<GetCandidateByIdQuery, CandidateDto>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<CandidateDto>> HandleAsync(
        GetCandidateByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var candidate = await _repository
            .GetByIdAsync(new CandidateId(query.Id), cancellationToken)
            .ConfigureAwait(false);

        if (candidate is null)
        {
            return Error.NotFound("candidate.not_found", $"No candidate found with id '{query.Id}'.");
        }

        return CandidateDto.FromDomain(candidate);
    }
}
