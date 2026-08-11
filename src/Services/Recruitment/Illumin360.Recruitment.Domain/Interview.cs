using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// An interview scheduled against an application. Owned + migration-managed by the service (unlike the
/// externally-seeded requests/applications). Carries an optional scorecard captured on completion.
/// </summary>
public sealed class Interview : Entity<InterviewId>
{
    // EF Core materialisation constructor.
    private Interview(InterviewId id) : base(id) { }

    /// <summary>The application being interviewed.</summary>
    public Guid ApplicationId { get; private set; }

    /// <summary>Scheduled start (UTC).</summary>
    public DateTimeOffset ScheduledAt { get; private set; }

    /// <summary>Duration in minutes.</summary>
    public int DurationMinutes { get; private set; }

    /// <summary>Location or mode (e.g. "Video call", an address).</summary>
    public string Location { get; private set; } = string.Empty;

    /// <summary>Optional round label (e.g. "Phone screen", "Technical", "Final").</summary>
    public string? Round { get; private set; }

    /// <summary>Skills this round is expected to assess, stored pipe-joined ("go|sql"). Empty if none.</summary>
    public string RequiredSkillsCsv { get; private set; } = string.Empty;

    /// <summary>The skills this round assesses (empty if none set).</summary>
    public IReadOnlyList<string> RequiredSkills =>
        string.IsNullOrEmpty(RequiredSkillsCsv) ? [] : RequiredSkillsCsv.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Lifecycle: <c>scheduled</c> / <c>completed</c> / <c>cancelled</c>.</summary>
    public string Status { get; private set; } = "scheduled";

    /// <summary>Scorecard rating (1–5) captured on completion, if any.</summary>
    public int? FeedbackRating { get; private set; }

    /// <summary>Scorecard comment captured on completion, if any.</summary>
    public string? FeedbackComment { get; private set; }

    /// <summary>When the interview was scheduled (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Schedules a new interview.</summary>
    /// <param name="applicationId">The application (required).</param>
    /// <param name="scheduledAt">Start time (must be in the future-ish; only non-default enforced here).</param>
    /// <param name="durationMinutes">Duration (1–480).</param>
    /// <param name="location">Location/mode (required).</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <param name="round">Optional round label.</param>
    /// <param name="requiredSkills">Optional skills this round assesses.</param>
    /// <returns>The interview, or a validation error.</returns>
    public static Result<Interview> Schedule(Guid applicationId, DateTimeOffset scheduledAt, int durationMinutes, string location, DateTimeOffset createdAt, string? round = null, IReadOnlyList<string>? requiredSkills = null)
    {
        if (applicationId == Guid.Empty)
        {
            return Error.Validation("interview.application_required", "An application id is required.");
        }

        if (scheduledAt == default)
        {
            return Error.Validation("interview.time_required", "A scheduled time is required.");
        }

        if (durationMinutes is < 1 or > 480)
        {
            return Error.Validation("interview.duration_invalid", "Duration must be between 1 and 480 minutes.");
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            return Error.Validation("interview.location_required", "A location or mode is required.");
        }

        var skills = (requiredSkills ?? [])
            .Select(s => s.Trim().ToLowerInvariant())
            .Where(s => s.Length > 0)
            .Distinct()
            .ToList();

        return new Interview(InterviewId.New())
        {
            ApplicationId = applicationId,
            ScheduledAt = scheduledAt,
            DurationMinutes = durationMinutes,
            Location = location.Trim(),
            Round = string.IsNullOrWhiteSpace(round) ? null : round.Trim(),
            RequiredSkillsCsv = string.Join('|', skills),
            Status = "scheduled",
            CreatedAt = createdAt,
        };
    }

    /// <summary>Records a scorecard and marks the interview completed.</summary>
    /// <param name="rating">Rating (1–5).</param>
    /// <param name="comment">Optional comment.</param>
    /// <returns>Success, a validation error for an out-of-range rating, or a conflict if not scheduled.</returns>
    public Result<Interview> RecordFeedback(int rating, string? comment)
    {
        if (!string.Equals(Status, "scheduled", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Conflict("interview.not_scheduled", "Only a scheduled interview can be completed.");
        }

        if (rating is < 1 or > 5)
        {
            return Error.Validation("interview.rating_invalid", "Rating must be between 1 and 5.");
        }

        Status = "completed";
        FeedbackRating = rating;
        FeedbackComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        return this;
    }

    /// <summary>Cancels a scheduled interview.</summary>
    /// <returns>Success, or a conflict if not scheduled.</returns>
    public Result<Interview> Cancel()
    {
        if (!string.Equals(Status, "scheduled", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Conflict("interview.not_scheduled", "Only a scheduled interview can be cancelled.");
        }

        Status = "cancelled";
        return this;
    }
}
