using System.Globalization;
using System.Text;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
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

/// <summary>Recruitment-owned feature snapshot for an application, gathered at decision time.</summary>
/// <param name="Source">Arrival channel.</param>
/// <param name="Remote">Whether the role is remote.</param>
/// <param name="InterviewCount">Number of interviews.</param>
/// <param name="AvgInterviewRating">Mean interview rating (1–5), if rated.</param>
/// <param name="HadOffer">Whether an offer was created.</param>
public sealed record OutcomeFeatureSnapshot(string Source, bool Remote, int InterviewCount, decimal? AvgInterviewRating, bool HadOffer);

/// <summary>Reads the captured hiring-outcome training set summary.</summary>
public sealed record GetMatchOutcomesQuery : IQuery<MatchOutcomeSummaryDto>;

/// <summary>Exports the captured hiring outcomes as a feature CSV (the LTR training set).</summary>
public sealed record GetOutcomesCsvQuery : IQuery<string>;

/// <summary>Pure CSV renderer for the labelled outcome feature rows (one header + one row per decision).</summary>
public static class OutcomesCsv
{
    /// <summary>Renders the outcomes as RFC-4180 CSV: features first, label (hired) last.</summary>
    /// <param name="rows">The captured outcomes.</param>
    /// <returns>The CSV text.</returns>
    public static string Render(IReadOnlyList<MatchOutcome> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var sb = new StringBuilder();
        sb.Append("application_id,request_id,talent_type,match_score,source,remote,interview_count,avg_interview_rating,had_offer,days_to_decision,decided_at,hired\r\n");
        foreach (var r in rows)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{r.ApplicationId},{r.RequestId},{Csv(r.TalentType)},{r.MatchScore},{Csv(r.Source)},{(r.Remote ? 1 : 0)},{r.InterviewCount},{r.AvgInterviewRating?.ToString(CultureInfo.InvariantCulture) ?? string.Empty},{(r.HadOffer ? 1 : 0)},{r.DaysToDecision},{r.DecidedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)},{(r.IsHire ? 1 : 0)}\r\n");
        }

        return sb.ToString();
    }

    private static string Csv(string value)
        => value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal)
            ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : value;
}

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

/// <summary>Handles <see cref="GetOutcomesCsvQuery"/> — renders the labelled feature rows as CSV.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetOutcomesCsvQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetOutcomesCsvQuery, string>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<string>> HandleAsync(GetOutcomesCsvQuery query, CancellationToken cancellationToken)
    {
        var outcomes = await _repository.ListMatchOutcomesAsync(cancellationToken).ConfigureAwait(false);
        return Result<string>.Success(OutcomesCsv.Render(outcomes));
    }
}
