using Illumin360.Matching;
using Illumin360.Recruitment.Application.Abstractions;
using Illumin360.Recruitment.Domain;
using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>A visible engagement review.</summary>
/// <param name="Id">Review id.</param>
/// <param name="Reviewer">Which side wrote it (Employer/Talent).</param>
/// <param name="Rating">Rating (1–5).</param>
/// <param name="Comment">Optional comment.</param>
/// <param name="CreatedAt">When it was written.</param>
public sealed record ReviewDto(Guid Id, string Reviewer, int Rating, string? Comment, DateTimeOffset CreatedAt);

/// <summary>A talent's reputation snapshot.</summary>
/// <param name="TalentId">The talent.</param>
/// <param name="Score">Reputation score (0–100).</param>
/// <param name="Count">Number of ratings.</param>
/// <param name="Average">Raw mean rating (1–5).</param>
public sealed record ReputationDto(Guid TalentId, int Score, int Count, double Average);

/// <summary>Leaves a review for one side of a hired application.</summary>
/// <param name="ApplicationId">The hired application.</param>
/// <param name="Reviewer">Which side is reviewing (Employer/Talent).</param>
/// <param name="Rating">Rating (1–5).</param>
/// <param name="Comment">Optional comment.</param>
public sealed record LeaveReviewCommand(Guid ApplicationId, string Reviewer, int Rating, string? Comment) : ICommand<ReviewDto>;

/// <summary>Lists the visible reviews for an application.</summary>
/// <param name="ApplicationId">The application.</param>
public sealed record GetApplicationReviewsQuery(Guid ApplicationId) : IQuery<IReadOnlyList<ReviewDto>>;

/// <summary>Gets a talent's reputation from their visible reviews.</summary>
/// <param name="TalentId">The talent.</param>
public sealed record GetTalentReputationQuery(Guid TalentId) : IQuery<ReputationDto>;

/// <summary>
/// Handles <see cref="LeaveReviewCommand"/> — records a side's review of a hire and, once both sides have
/// reviewed, reveals both (double-blind).
/// </summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class LeaveReviewCommandHandler(IRecruitmentRepository repository)
    : ICommandHandler<LeaveReviewCommand, ReviewDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ReviewDto>> HandleAsync(LeaveReviewCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!Enum.TryParse<ReviewerSide>(command.Reviewer, ignoreCase: true, out var side))
        {
            return Error.Validation("review.reviewer_invalid", "Reviewer must be 'employer' or 'talent'.");
        }

        var application = await _repository.GetApplicationAsync(new Domain.ApplicationId(command.ApplicationId), cancellationToken).ConfigureAwait(false);
        if (application is null)
        {
            return Error.NotFound("application.not_found", "Application not found.");
        }

        if (!application.IsHire)
        {
            return Error.Validation("review.not_hired", "Only a completed hire can be reviewed.");
        }

        if (await _repository.GetReviewAsync(command.ApplicationId, side, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Error.Conflict("review.already_left", "You have already reviewed this engagement.");
        }

        var review = EngagementReview.Create(command.ApplicationId, application.RequestId.Value, application.TalentId, side, command.Rating, command.Comment, DateTimeOffset.UtcNow);
        if (review.IsFailure)
        {
            return review.Error!;
        }

        _repository.AddEngagementReview(review.Value!);

        // Double-blind: reveal both sides once the counterparty has also reviewed.
        var otherSide = side == ReviewerSide.Employer ? ReviewerSide.Talent : ReviewerSide.Employer;
        var counterpart = await _repository.GetReviewAsync(command.ApplicationId, otherSide, cancellationToken).ConfigureAwait(false);
        if (counterpart is not null)
        {
            review.Value!.Reveal();
            counterpart.Reveal();
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var r = review.Value!;
        return new ReviewDto(r.Id, r.Reviewer.ToString(), r.Rating, r.Comment, r.CreatedAt);
    }
}

/// <summary>Handles <see cref="GetApplicationReviewsQuery"/> — visible reviews only.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetApplicationReviewsQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetApplicationReviewsQuery, IReadOnlyList<ReviewDto>>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ReviewDto>>> HandleAsync(GetApplicationReviewsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var reviews = await _repository.ListReviewsForApplicationAsync(query.ApplicationId, cancellationToken).ConfigureAwait(false);
        return reviews
            .Where(r => r.Visible)
            .Select(r => new ReviewDto(r.Id, r.Reviewer.ToString(), r.Rating, r.Comment, r.CreatedAt))
            .ToList();
    }
}

/// <summary>Handles <see cref="GetTalentReputationQuery"/> — reputation from visible employer→talent reviews.</summary>
/// <param name="repository">The recruitment repository.</param>
public sealed class GetTalentReputationQueryHandler(IRecruitmentRepository repository)
    : IQueryHandler<GetTalentReputationQuery, ReputationDto>
{
    private readonly IRecruitmentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<ReputationDto>> HandleAsync(GetTalentReputationQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var reviews = await _repository.ListReviewsForTalentAsync(query.TalentId, cancellationToken).ConfigureAwait(false);

        // A talent's reputation is what employers rated them (visible only).
        var ratings = reviews.Where(r => r.Visible && r.Reviewer == ReviewerSide.Employer).Select(r => r.Rating);
        var snapshot = ReputationScorer.Score(ratings);
        return new ReputationDto(query.TalentId, snapshot.Score, snapshot.Count, snapshot.Average);
    }
}
