using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>
/// Aggregate view of captured hiring outcomes — the labelled dataset that a future learning-to-rank model
/// will train on, and a live read on whether the current ranker's score actually separates hires from
/// rejections (avg score for hires should exceed avg score for rejections).
/// </summary>
/// <param name="Total">Total decided applications captured.</param>
/// <param name="Hired">How many were hired.</param>
/// <param name="Rejected">How many were rejected.</param>
/// <param name="AvgScoreHired">Mean match score of hires (0 when none).</param>
/// <param name="AvgScoreRejected">Mean match score of rejections (0 when none).</param>
public sealed record MatchOutcomeSummaryDto(int Total, int Hired, int Rejected, double AvgScoreHired, double AvgScoreRejected);

/// <summary>Reads the captured hiring-outcome training set summary.</summary>
public sealed record GetMatchOutcomesQuery : IQuery<MatchOutcomeSummaryDto>;

/// <summary>Handles <see cref="GetMatchOutcomesQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetMatchOutcomesQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetMatchOutcomesQuery, MatchOutcomeSummaryDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<MatchOutcomeSummaryDto>> HandleAsync(GetMatchOutcomesQuery query, CancellationToken cancellationToken)
    {
        var outcomes = await _repository.ListMatchOutcomesAsync(cancellationToken).ConfigureAwait(false);
        var hired = outcomes.Where(o => o.IsHire).ToList();
        var rejected = outcomes.Where(o => !o.IsHire).ToList();

        static double Avg(IReadOnlyList<decimal> scores) => scores.Count == 0 ? 0 : Math.Round((double)scores.Average(), 1);

        return new MatchOutcomeSummaryDto(
            outcomes.Count,
            hired.Count,
            rejected.Count,
            Avg(hired.Select(o => o.MatchScore).ToList()),
            Avg(rejected.Select(o => o.MatchScore).ToList()));
    }
}
