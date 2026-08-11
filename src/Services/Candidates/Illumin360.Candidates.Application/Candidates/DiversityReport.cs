using Illumin360.Candidates.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>
/// An anonymised diversity / EEO snapshot of the candidate pool — aggregate counts only, never
/// individual records, so it can be shared without exposing personal data.
/// </summary>
/// <param name="Total">Total candidates in the pool.</param>
/// <param name="ByNationality">Candidate counts grouped by nationality.</param>
/// <param name="ByCity">Candidate counts grouped by city.</param>
/// <param name="ByAvailability">Candidate counts grouped by availability status.</param>
public sealed record DiversityReportDto(
    int Total,
    IReadOnlyList<CountByLabel> ByNationality,
    IReadOnlyList<CountByLabel> ByCity,
    IReadOnlyList<CountByLabel> ByAvailability);

/// <summary>Query: an anonymised diversity report over the candidate pool.</summary>
public sealed record GetDiversityReportQuery : IQuery<DiversityReportDto>;

/// <summary>Handles <see cref="GetDiversityReportQuery"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class GetDiversityReportQueryHandler(ICandidateRepository repository)
    : IQueryHandler<GetDiversityReportQuery, DiversityReportDto>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<DiversityReportDto>> HandleAsync(GetDiversityReportQuery query, CancellationToken cancellationToken)
    {
        var report = await _repository.GetDiversityReportAsync(cancellationToken).ConfigureAwait(false);
        return Result<DiversityReportDto>.Success(report);
    }
}
