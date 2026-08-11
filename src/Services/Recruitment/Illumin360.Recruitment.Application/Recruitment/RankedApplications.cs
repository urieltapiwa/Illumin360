using Illumin360.Matching;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>An application with both the heuristic match score and the learned-model score.</summary>
/// <param name="ApplicationId">Application id.</param>
/// <param name="TalentType">Talent type.</param>
/// <param name="Status">Pipeline status.</param>
/// <param name="MatchScore">The heuristic match score (0–100).</param>
/// <param name="LearnedScore">The learned model's hire-likelihood score (0–100); equals MatchScore when the model isn't used.</param>
public sealed record RankedApplicationDto(Guid ApplicationId, string TalentType, string Status, decimal MatchScore, int LearnedScore);

/// <summary>A requisition's applications ranked either by the learned model or (fallback) the heuristic.</summary>
/// <param name="UsedModel">True when the learned model drove the ranking.</param>
/// <param name="Message">Why the learned model was or wasn't used.</param>
/// <param name="Applications">The applications, best-first.</param>
public sealed record RankedApplicationsDto(bool UsedModel, string Message, IReadOnlyList<RankedApplicationDto> Applications);

/// <summary>Ranks a requisition's applications — by the learned model when enabled + trustworthy, else by the heuristic.</summary>
/// <param name="RequestId">The requisition id.</param>
/// <param name="UseModel">Whether the caller enabled learned ranking (the feature flag).</param>
public sealed record GetRankedApplicationsQuery(Guid RequestId, bool UseModel) : IQuery<RankedApplicationsDto>;

/// <summary>Handles <see cref="GetRankedApplicationsQuery"/> — live scoring with the learned ranker.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetRankedApplicationsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetRankedApplicationsQuery, RankedApplicationsDto>
{
    private const int MinSamples = 20;

    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<RankedApplicationsDto>> HandleAsync(GetRankedApplicationsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var apps = await _repository.ListApplicationsAsync(new RequestId(query.RequestId), 0, 200, cancellationToken).ConfigureAwait(false);

        // Heuristic fallback: match-score order, learned == heuristic.
        RankedApplicationsDto Heuristic(string message) => new(
            false,
            message,
            apps.OrderByDescending(a => a.MatchScore)
                .Select(a => new RankedApplicationDto(a.Id.Value, a.TalentType, a.Status, a.MatchScore, (int)Math.Round(a.MatchScore)))
                .ToList());

        if (!query.UseModel)
        {
            return Heuristic("Learned ranking disabled — showing heuristic order.");
        }

        var outcomes = await _repository.ListMatchOutcomesAsync(cancellationToken).ConfigureAwait(false);
        if (outcomes.Count < MinSamples || outcomes.All(o => o.IsHire) || outcomes.All(o => !o.IsHire))
        {
            return Heuristic($"Not enough labelled decisions to trust a model yet ({outcomes.Count}) — using heuristic.");
        }

        var samples = outcomes.Select(o => new RankSample(OutcomeFeatures.Vector(o), o.IsHire ? 1 : 0)).ToList();
        var evaluation = RankEvaluator.Evaluate(samples, f => f[0]);
        if (evaluation is null || !evaluation.BetterThanBaseline)
        {
            return Heuristic($"Learned model doesn't beat the heuristic yet (model AUC {evaluation?.ModelAuc ?? 0} vs {evaluation?.BaselineAuc ?? 0}) — using heuristic.");
        }

        var model = LogisticRegressionTrainer.Train(samples);
        var now = DateTimeOffset.UtcNow;

        // Score each application from its current (in-pipeline) feature snapshot.
        var scored = new List<RankedApplicationDto>();
        foreach (var a in apps)
        {
            var f = await _repository.GetOutcomeFeaturesAsync(a.Id.Value, query.RequestId, cancellationToken).ConfigureAwait(false)
                ?? new OutcomeFeatureSnapshot("direct", false, 0, null, false, 0, 0, 0);
            var days = (int)(now - a.AppliedAt).TotalDays;
            var vector = OutcomeFeatures.VectorOf(a.MatchScore, f.Remote, f.InterviewCount, f.AvgInterviewRating, f.HadOffer, days, f.CitySignal, f.RoleSignal, f.SkillSignal);
            scored.Add(new RankedApplicationDto(a.Id.Value, a.TalentType, a.Status, a.MatchScore, model.Score(vector)));
        }

        return new RankedApplicationsDto(
            true,
            $"Ranked by the learned model (test AUC {evaluation.ModelAuc} vs heuristic {evaluation.BaselineAuc}).",
            scored.OrderByDescending(s => s.LearnedScore).ThenByDescending(s => s.MatchScore).ToList());
    }
}
