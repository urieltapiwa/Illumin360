using System.Globalization;
using System.Text;
using Illumin360.Matching;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>Maps a captured outcome to the LTR feature vector (shared by training + serving).</summary>
public static class OutcomeFeatures
{
    /// <summary>Ordered feature names (must line up with <see cref="Vector"/>).</summary>
    public static readonly IReadOnlyList<string> Names =
        ["matchScore", "remote", "interviewCount", "avgInterviewRating", "hadOffer", "daysToDecision", "citySignal", "roleSignal", "skillSignal"];

    /// <summary>The numeric feature vector for a captured outcome (scaled to roughly 0–1 ranges).</summary>
    /// <param name="o">The outcome.</param>
    /// <returns>The feature vector, ordered to match <see cref="Names"/>.</returns>
    public static double[] Vector(MatchOutcome o)
    {
        ArgumentNullException.ThrowIfNull(o);
        return VectorOf(o.MatchScore, o.Remote, o.InterviewCount, o.AvgInterviewRating, o.HadOffer, o.DaysToDecision, o.CitySignal, o.RoleSignal, o.SkillSignal);
    }

    /// <summary>Builds the feature vector from raw signals (shared by training + live scoring).</summary>
    /// <param name="matchScore">Composite match score (0–100).</param>
    /// <param name="remote">Whether the role is remote.</param>
    /// <param name="interviewCount">Interviews so far.</param>
    /// <param name="avgInterviewRating">Mean interview rating (1–5), if any.</param>
    /// <param name="hadOffer">Whether an offer exists.</param>
    /// <param name="daysElapsed">Days since apply (live) / to decision (training).</param>
    /// <param name="citySignal">Talent-side city-fit (0–100).</param>
    /// <param name="roleSignal">Talent-side role-affinity (0–100).</param>
    /// <param name="skillSignal">Talent-side skill-fit (0–100).</param>
    /// <returns>The feature vector, ordered to match <see cref="Names"/>.</returns>
    public static double[] VectorOf(decimal matchScore, bool remote, int interviewCount, decimal? avgInterviewRating, bool hadOffer, int daysElapsed, int citySignal, int roleSignal, int skillSignal)
        =>
        [
            (double)matchScore / 100.0,
            remote ? 1.0 : 0.0,
            Math.Min(interviewCount, 10) / 10.0,
            (double)(avgInterviewRating ?? 0m) / 5.0,
            hadOffer ? 1.0 : 0.0,
            Math.Min(daysElapsed, 60) / 60.0,
            citySignal / 100.0,
            roleSignal / 100.0,
            skillSignal / 100.0,
        ];
}

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
/// <param name="CitySignal">Talent-side city-fit signal captured at apply-time (0–100).</param>
/// <param name="RoleSignal">Talent-side role-affinity signal (0–100).</param>
/// <param name="SkillSignal">Talent-side skill-fit signal (0–100).</param>
public sealed record OutcomeFeatureSnapshot(string Source, bool Remote, int InterviewCount, decimal? AvgInterviewRating, bool HadOffer, int CitySignal, int RoleSignal, int SkillSignal);

/// <summary>Reads the captured hiring-outcome training set summary.</summary>
public sealed record GetMatchOutcomesQuery : IQuery<MatchOutcomeSummaryDto>;

/// <summary>Exports the captured hiring outcomes as a feature CSV (the LTR training set).</summary>
public sealed record GetOutcomesCsvQuery : IQuery<string>;

/// <summary>A learned feature weight (interpretability).</summary>
/// <param name="Feature">Feature name.</param>
/// <param name="Weight">Standardised-space weight (sign = direction, magnitude = influence).</param>
public sealed record RankWeightDto(string Feature, double Weight);

/// <summary>
/// The trained learning-to-rank model report: whether a model could be trained (needs enough labelled
/// decisions of both classes), its hold-out metrics vs the current match-score heuristic, and the learned
/// per-feature weights. Train + evaluate + serve, on demand from the captured outcomes.
/// </summary>
/// <param name="Trained">Whether a model was trained + evaluated.</param>
/// <param name="Message">Human-readable status (e.g. why not trained).</param>
/// <param name="SampleCount">Total labelled samples.</param>
/// <param name="Hired">Positive labels.</param>
/// <param name="Rejected">Negative labels.</param>
/// <param name="ModelAuc">Learned-model AUC on held-out data.</param>
/// <param name="BaselineAuc">Current heuristic AUC on the same held-out data.</param>
/// <param name="Accuracy">Model accuracy at a 0.5 threshold.</param>
/// <param name="LogLoss">Model log-loss.</param>
/// <param name="BetterThanBaseline">Whether the learned model out-ranks the heuristic.</param>
/// <param name="Weights">Learned per-feature weights.</param>
public sealed record RankModelReportDto(
    bool Trained,
    string Message,
    int SampleCount,
    int Hired,
    int Rejected,
    double ModelAuc,
    double BaselineAuc,
    double Accuracy,
    double LogLoss,
    bool BetterThanBaseline,
    IReadOnlyList<RankWeightDto> Weights);

/// <summary>Trains + evaluates a learning-to-rank model on the captured outcomes (on demand).</summary>
public sealed record GetRankModelQuery : IQuery<RankModelReportDto>;

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
        sb.Append("application_id,request_id,talent_type,match_score,source,remote,interview_count,avg_interview_rating,had_offer,days_to_decision,city_signal,role_signal,skill_signal,decided_at,hired\r\n");
        foreach (var r in rows)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{r.ApplicationId},{r.RequestId},{Csv(r.TalentType)},{r.MatchScore},{Csv(r.Source)},{(r.Remote ? 1 : 0)},{r.InterviewCount},{r.AvgInterviewRating?.ToString(CultureInfo.InvariantCulture) ?? string.Empty},{(r.HadOffer ? 1 : 0)},{r.DaysToDecision},{r.CitySignal},{r.RoleSignal},{r.SkillSignal},{r.DecidedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)},{(r.IsHire ? 1 : 0)}\r\n");
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

/// <summary>Handles <see cref="GetRankModelQuery"/> — trains + evaluates a ranker on the captured outcomes.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetRankModelQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetRankModelQuery, RankModelReportDto>
{
    // Below this many labelled decisions (of both classes) a learned model isn't trustworthy.
    private const int MinSamples = 20;

    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<RankModelReportDto>> HandleAsync(GetRankModelQuery query, CancellationToken cancellationToken)
    {
        var outcomes = await _repository.ListMatchOutcomesAsync(cancellationToken).ConfigureAwait(false);
        var hired = outcomes.Count(o => o.IsHire);
        var rejected = outcomes.Count - hired;

        RankModelReportDto NotTrained(string message) =>
            new(false, message, outcomes.Count, hired, rejected, 0, 0, 0, 0, false, []);

        if (outcomes.Count < MinSamples)
        {
            return NotTrained($"Need at least {MinSamples} decisions to train (have {outcomes.Count}).");
        }

        if (hired == 0 || rejected == 0)
        {
            return NotTrained("Need both hires and rejections to train a ranker.");
        }

        var samples = outcomes.Select(o => new RankSample(OutcomeFeatures.Vector(o), o.IsHire ? 1 : 0)).ToList();

        // Baseline = the current heuristic (match score is feature 0).
        var evaluation = RankEvaluator.Evaluate(samples, f => f[0]);
        if (evaluation is null)
        {
            return NotTrained("Not enough class variety in the hold-out split yet.");
        }

        var model = LogisticRegressionTrainer.Train(samples);
        var weights = OutcomeFeatures.Names
            .Select((name, i) => new RankWeightDto(name, Math.Round(i < model.Weights.Length ? model.Weights[i] : 0, 3)))
            .ToList();

        var message = evaluation.BetterThanBaseline
            ? $"Learned ranker out-ranks the heuristic (AUC {evaluation.ModelAuc} vs {evaluation.BaselineAuc})."
            : $"Heuristic still competitive (model AUC {evaluation.ModelAuc} vs {evaluation.BaselineAuc}); keep collecting data.";

        return new RankModelReportDto(
            true,
            message,
            outcomes.Count,
            hired,
            rejected,
            evaluation.ModelAuc,
            evaluation.BaselineAuc,
            evaluation.Accuracy,
            evaluation.LogLoss,
            evaluation.BetterThanBaseline,
            weights);
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
