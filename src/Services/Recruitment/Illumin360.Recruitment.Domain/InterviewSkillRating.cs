using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// A per-skill score (1–5) captured for an interview round. Service-owned + migration-managed, keyed by
/// interview id. Aggregated across an application's rounds to give a per-skill picture of the candidate.
/// </summary>
public sealed class InterviewSkillRating : Entity<Guid>
{
    private InterviewSkillRating(Guid id)
        : base(id)
    {
    }

    /// <summary>The interview this rating was captured in.</summary>
    public Guid InterviewId { get; private init; }

    /// <summary>The skill rated (normalised, lower-cased).</summary>
    public string Skill { get; private set; } = string.Empty;

    /// <summary>The score, 1–5.</summary>
    public int Rating { get; private set; }

    /// <summary>When the rating was captured (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Records a per-skill rating.</summary>
    /// <param name="interviewId">The interview (required).</param>
    /// <param name="skill">The skill (required, normalised).</param>
    /// <param name="rating">The score (1–5).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The rating, or a validation error.</returns>
    public static Result<InterviewSkillRating> Create(Guid interviewId, string skill, int rating, DateTimeOffset createdAt)
    {
        if (interviewId == Guid.Empty)
        {
            return Error.Validation("skillrating.interview_required", "An interview id is required.");
        }

        if (string.IsNullOrWhiteSpace(skill))
        {
            return Error.Validation("skillrating.skill_required", "A skill is required.");
        }

        if (rating is < 1 or > 5)
        {
            return Error.Validation("skillrating.rating_invalid", "Rating must be between 1 and 5.");
        }

        return new InterviewSkillRating(Guid.NewGuid())
        {
            InterviewId = interviewId,
            Skill = skill.Trim().ToLowerInvariant(),
            Rating = rating,
            CreatedAt = createdAt,
        };
    }
}
