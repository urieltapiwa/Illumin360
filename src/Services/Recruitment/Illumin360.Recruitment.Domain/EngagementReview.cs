using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>Which side of a hire left a review.</summary>
public enum ReviewerSide
{
    /// <summary>The hiring employer reviewing the talent.</summary>
    Employer,

    /// <summary>The talent reviewing the employer.</summary>
    Talent,
}

/// <summary>
/// A two-sided review of a completed engagement (a <see cref="RecruitmentApplication"/> that reached hire).
/// Reviews are double-blind: neither side's review is <see cref="Visible"/> until both have submitted, so one
/// party can't tailor theirs to the other's. Phase 0 of the marketplace trust layer (see the transaction-layer
/// design doc); anchored to hires today, extends to contract completion when the Payments service lands.
/// </summary>
public sealed class EngagementReview : Entity<Guid>
{
    private EngagementReview(Guid id)
        : base(id)
    {
    }

    /// <summary>The hired application being reviewed.</summary>
    public Guid ApplicationId { get; private set; }

    /// <summary>The requisition the application belonged to.</summary>
    public Guid RequestId { get; private set; }

    /// <summary>The talent involved (the review subject when the employer reviews).</summary>
    public Guid TalentId { get; private set; }

    /// <summary>Which side wrote this review.</summary>
    public ReviewerSide Reviewer { get; private set; }

    /// <summary>Rating, 1–5.</summary>
    public int Rating { get; private set; }

    /// <summary>Optional free-text comment.</summary>
    public string? Comment { get; private set; }

    /// <summary>Whether the review is publicly visible (unlocked once both sides have reviewed).</summary>
    public bool Visible { get; private set; }

    /// <summary>When the review was written (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Records a review for one side of a hire.</summary>
    /// <param name="applicationId">The hired application.</param>
    /// <param name="requestId">The requisition id.</param>
    /// <param name="talentId">The talent id.</param>
    /// <param name="reviewer">Which side is reviewing.</param>
    /// <param name="rating">Rating (1–5).</param>
    /// <param name="comment">Optional comment (≤ 2000 chars).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The review, or a validation error.</returns>
    public static Result<EngagementReview> Create(Guid applicationId, Guid requestId, Guid talentId, ReviewerSide reviewer, int rating, string? comment, DateTimeOffset createdAt)
    {
        if (rating is < 1 or > 5)
        {
            return Error.Validation("review.rating_invalid", "Rating must be between 1 and 5.");
        }

        if (comment is { Length: > 2000 })
        {
            return Error.Validation("review.comment_too_long", "The comment is too long.");
        }

        return new EngagementReview(Guid.NewGuid())
        {
            ApplicationId = applicationId,
            RequestId = requestId,
            TalentId = talentId,
            Reviewer = reviewer,
            Rating = rating,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            Visible = false,
            CreatedAt = createdAt,
        };
    }

    /// <summary>Reveals the review (called once both sides have submitted).</summary>
    public void Reveal() => Visible = true;
}
