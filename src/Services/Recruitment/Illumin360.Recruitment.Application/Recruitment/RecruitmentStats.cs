using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A labelled count — a bucket label and its total.</summary>
/// <param name="Label">The bucket label.</param>
/// <param name="Count">Number of items in the bucket.</param>
public sealed record CountByLabel(string Label, int Count);

/// <summary>A monthly funnel point — applications and hires for one calendar month.</summary>
/// <param name="Month">Month key in <c>yyyy-MM</c> form.</param>
/// <param name="Applications">Applications made that month.</param>
/// <param name="Hires">Hires made that month.</param>
public sealed record MonthlyPoint(string Month, int Applications, int Hires);

/// <summary>Aggregate statistics over recruitment activity, for dashboards and reporting.</summary>
/// <param name="TotalRequests">Total recruitment requests.</param>
/// <param name="OpenRequests">Requests still open.</param>
/// <param name="FilledRequests">Requests filled.</param>
/// <param name="TotalApplications">Total applications received.</param>
/// <param name="TotalHires">Total hires made.</param>
/// <param name="AvgMatchScore">Average match score across all applications.</param>
/// <param name="Funnel">Application counts by pipeline stage (applied → … → hired).</param>
/// <param name="ByTalentType">Application counts by talent type.</param>
/// <param name="TopCities">Top cities by request count.</param>
/// <param name="Trend">Applications and hires per month.</param>
public sealed record RecruitmentStatsDto(
    int TotalRequests,
    int OpenRequests,
    int FilledRequests,
    int TotalApplications,
    int TotalHires,
    double AvgMatchScore,
    IReadOnlyList<CountByLabel> Funnel,
    IReadOnlyList<CountByLabel> ByTalentType,
    IReadOnlyList<CountByLabel> TopCities,
    IReadOnlyList<MonthlyPoint> Trend);

/// <summary>Query: aggregate statistics over recruitment activity.</summary>
public sealed record GetRecruitmentStatsQuery : IQuery<RecruitmentStatsDto>;

/// <summary>Handles <see cref="GetRecruitmentStatsQuery"/> by reading aggregate counts from the repository.</summary>
public sealed class GetRecruitmentStatsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetRecruitmentStatsQuery, RecruitmentStatsDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<RecruitmentStatsDto>> HandleAsync(
        GetRecruitmentStatsQuery query, CancellationToken cancellationToken)
    {
        var stats = await _repository.GetStatsAsync(cancellationToken).ConfigureAwait(false);
        return Result<RecruitmentStatsDto>.Success(stats);
    }
}
