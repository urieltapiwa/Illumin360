using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// A talent's application against a <see cref="RecruitmentRequest"/>. Within this bounded context it is a
/// read-only projection: applications are produced by the matching pipeline and the historical seed, so the
/// service surfaces them for funnel and matching analytics rather than mutating them here.
/// </summary>
public sealed class RecruitmentApplication : Entity<ApplicationId>
{
    // EF Core materialisation constructor.
    private RecruitmentApplication(ApplicationId id) : base(id) { }

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
}
