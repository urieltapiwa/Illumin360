using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A single per-skill score being submitted.</summary>
/// <param name="Skill">The skill.</param>
/// <param name="Rating">The score (1–5).</param>
public sealed record SkillRatingInput(string Skill, int Rating);

/// <summary>A per-skill score recorded for an interview.</summary>
/// <param name="Skill">The skill.</param>
/// <param name="Rating">The score (1–5).</param>
public sealed record SkillRatingDto(string Skill, int Rating);

/// <summary>An aggregated per-skill score across an application's completed rounds.</summary>
/// <param name="Skill">The skill.</param>
/// <param name="Average">Mean score (1 dp).</param>
/// <param name="Count">Number of ratings contributing.</param>
public sealed record SkillAverageDto(string Skill, double Average, int Count);

/// <summary>A round in the application's interview summary.</summary>
/// <param name="InterviewId">The interview id.</param>
/// <param name="Round">Round label, if any.</param>
/// <param name="ScheduledAt">Start (UTC).</param>
/// <param name="Status">scheduled/completed/cancelled.</param>
/// <param name="OverallRating">Overall scorecard rating, if completed.</param>
public sealed record RoundSummaryDto(Guid InterviewId, string? Round, DateTimeOffset ScheduledAt, string Status, int? OverallRating);

/// <summary>An application's multi-round interview picture: the rounds + per-skill averages across them.</summary>
/// <param name="Rounds">Rounds in chronological order.</param>
/// <param name="SkillAverages">Per-skill averages across all rounds (descending by average).</param>
public sealed record InterviewSummaryDto(IReadOnlyList<RoundSummaryDto> Rounds, IReadOnlyList<SkillAverageDto> SkillAverages);

/// <summary>Lists an interview's per-skill ratings.</summary>
/// <param name="InterviewId">The interview id.</param>
public sealed record GetSkillRatingsQuery(Guid InterviewId) : IQuery<IReadOnlyList<SkillRatingDto>>;

/// <summary>Replaces an interview's per-skill ratings.</summary>
/// <param name="InterviewId">The interview id.</param>
/// <param name="Ratings">The ratings.</param>
public sealed record RecordSkillRatingsCommand(Guid InterviewId, IReadOnlyList<SkillRatingInput> Ratings) : ICommand<IReadOnlyList<SkillRatingDto>>;

/// <summary>Aggregates an application's interview rounds and per-skill scores.</summary>
/// <param name="ApplicationId">The application id.</param>
public sealed record GetInterviewSummaryQuery(Guid ApplicationId) : IQuery<InterviewSummaryDto>;

/// <summary>Handles <see cref="GetSkillRatingsQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetSkillRatingsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetSkillRatingsQuery, IReadOnlyList<SkillRatingDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SkillRatingDto>>> HandleAsync(GetSkillRatingsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var ratings = await _repository.ListSkillRatingsAsync(query.InterviewId, cancellationToken).ConfigureAwait(false);
        return ratings.Select(r => new SkillRatingDto(r.Skill, r.Rating)).ToList();
    }
}

/// <summary>Handles <see cref="RecordSkillRatingsCommand"/> — replaces the interview's skill ratings.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class RecordSkillRatingsCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<RecordSkillRatingsCommand, IReadOnlyList<SkillRatingDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SkillRatingDto>>> HandleAsync(RecordSkillRatingsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var interview = await _repository.GetInterviewAsync(new InterviewId(command.InterviewId), cancellationToken).ConfigureAwait(false);
        if (interview is null)
        {
            return Error.NotFound("interview.not_found", "No matching interview was found.");
        }

        var now = DateTimeOffset.UtcNow;
        var created = new List<InterviewSkillRating>();
        foreach (var input in command.Ratings ?? [])
        {
            var creation = InterviewSkillRating.Create(command.InterviewId, input.Skill, input.Rating, now);
            if (creation.IsFailure)
            {
                return creation.Error!;
            }

            created.Add(creation.Value!);
        }

        // Replace any prior ratings for this interview (idempotent re-submit).
        var existing = await _repository.ListSkillRatingsTrackedAsync(command.InterviewId, cancellationToken).ConfigureAwait(false);
        foreach (var prior in existing)
        {
            _repository.RemoveSkillRating(prior);
        }

        foreach (var rating in created)
        {
            _repository.AddSkillRating(rating);
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return created.Select(r => new SkillRatingDto(r.Skill, r.Rating)).ToList();
    }
}

/// <summary>Handles <see cref="GetInterviewSummaryQuery"/>.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetInterviewSummaryQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetInterviewSummaryQuery, InterviewSummaryDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<InterviewSummaryDto>> HandleAsync(GetInterviewSummaryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var interviews = await _repository.ListInterviewsForApplicationAsync(query.ApplicationId, cancellationToken).ConfigureAwait(false);
        var ordered = interviews.OrderBy(i => i.ScheduledAt).ToList();

        var rounds = ordered
            .Select(i => new RoundSummaryDto(i.Id.Value, i.Round, i.ScheduledAt, i.Status, i.FeedbackRating))
            .ToList();

        var averages = new List<SkillAverageDto>();
        var ids = ordered.Select(i => i.Id.Value).ToList();
        if (ids.Count > 0)
        {
            var ratings = await _repository.ListSkillRatingsForInterviewsAsync(ids, cancellationToken).ConfigureAwait(false);
            averages = ratings
                .GroupBy(r => r.Skill)
                .Select(g => new SkillAverageDto(g.Key, Math.Round(g.Average(x => x.Rating), 1), g.Count()))
                .OrderByDescending(s => s.Average)
                .ThenBy(s => s.Skill, StringComparer.Ordinal)
                .ToList();
        }

        return new InterviewSummaryDto(rounds, averages);
    }
}
