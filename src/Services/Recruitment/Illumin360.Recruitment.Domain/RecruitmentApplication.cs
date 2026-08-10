using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// A talent's application against a <see cref="RecruitmentRequest"/>. Historical applications are produced
/// by the matching pipeline and the decade-long seed and surfaced read-only for funnel/matching analytics;
/// a signed-in talent applying from a portal creates a fresh row via <see cref="Apply"/>.
/// </summary>
public sealed class RecruitmentApplication : Entity<ApplicationId>
{
    // EF Core materialisation constructor.
    private RecruitmentApplication(ApplicationId id) : base(id) { }

    /// <summary>Records a fresh application from a talent applying to an open role via a portal.</summary>
    /// <param name="requestId">The request being applied to.</param>
    /// <param name="talentId">The applying talent's id.</param>
    /// <param name="talentType">Talent type (<c>student</c> or <c>professional</c>).</param>
    /// <param name="appliedAt">When the application was made (UTC).</param>
    /// <returns>A new application in the <c>applied</c> stage.</returns>
    public static RecruitmentApplication Apply(RequestId requestId, Guid talentId, string talentType, DateTimeOffset appliedAt)
        => new(ApplicationId.New())
        {
            RequestId = requestId,
            TalentId = talentId,
            TalentType = talentType,
            MatchScore = 0m,
            Status = "applied",
            IsHire = false,
            AppliedAt = appliedAt,
        };

    /// <summary>The request this application targets.</summary>
    public RequestId RequestId { get; private set; }

    /// <summary>The applying talent's id.</summary>
    public Guid TalentId { get; private set; }

    /// <summary>Talent type (<c>student</c> or <c>professional</c>).</summary>
    public string TalentType { get; private set; } = string.Empty;

    /// <summary>Match score (0–100) from the matching engine.</summary>
    public decimal MatchScore { get; private set; }

    /// <summary>Pipeline status (<c>applied</c>, <c>reviewed</c>, <c>shortlisted</c>, <c>hired</c>, <c>rejected</c>).</summary>
    public string Status { get; private set; } = string.Empty;

    /// <summary>Whether this application resulted in a hire.</summary>
    public bool IsHire { get; private set; }

    /// <summary>When the application was made (UTC).</summary>
    public DateTimeOffset AppliedAt { get; private set; }

    /// <summary>When a decision was reached (UTC), if any.</summary>
    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>Ordered non-terminal pipeline stages a recruiter advances an application through.</summary>
    private static readonly string[] Stages = ["applied", "reviewed", "shortlisted", "hired"];

    /// <summary>Whether the application has reached a terminal decision (hired or rejected).</summary>
    public bool IsDecided => IsHire || string.Equals(Status, "rejected", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Advances the application to the next pipeline stage (applied → reviewed → shortlisted → hired).
    /// Reaching <c>hired</c> is a terminal decision.
    /// </summary>
    /// <param name="decidedAt">Timestamp used if this advance reaches a terminal decision (UTC).</param>
    /// <returns>Success, or a conflict if already decided.</returns>
    public Result<RecruitmentApplication> Advance(DateTimeOffset decidedAt)
    {
        if (IsDecided)
        {
            return Error.Conflict("application.already_decided", "This application has already been decided.");
        }

        var current = Array.FindIndex(Stages, s => string.Equals(s, Status, StringComparison.OrdinalIgnoreCase));
        var nextIndex = current < 0 ? 1 : Math.Min(current + 1, Stages.Length - 1);
        Status = Stages[nextIndex];

        if (string.Equals(Status, "hired", StringComparison.OrdinalIgnoreCase))
        {
            IsHire = true;
            DecidedAt = decidedAt;
        }

        return this;
    }

    /// <summary>Rejects the application (terminal).</summary>
    /// <param name="decidedAt">When the decision was reached (UTC).</param>
    /// <returns>Success, or a conflict if already decided.</returns>
    public Result<RecruitmentApplication> Reject(DateTimeOffset decidedAt)
    {
        if (IsDecided)
        {
            return Error.Conflict("application.already_decided", "This application has already been decided.");
        }

        Status = "rejected";
        DecidedAt = decidedAt;
        return this;
    }
}
