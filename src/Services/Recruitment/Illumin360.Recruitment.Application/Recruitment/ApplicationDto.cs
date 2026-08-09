using Illumin360.Recruitment.Domain;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>Read model returned to callers for an application against a request.</summary>
/// <param name="Id">Application id.</param>
/// <param name="RequestId">The request this application targets.</param>
/// <param name="TalentId">The applying talent's id.</param>
/// <param name="TalentType">Talent type (<c>student</c>/<c>professional</c>).</param>
/// <param name="MatchScore">Match score (0–100).</param>
/// <param name="Status">Pipeline status.</param>
/// <param name="IsHire">Whether the application resulted in a hire.</param>
/// <param name="AppliedAt">When applied (UTC).</param>
/// <param name="DecidedAt">When decided (UTC), if any.</param>
public sealed record ApplicationDto(
    Guid Id,
    Guid RequestId,
    Guid TalentId,
    string TalentType,
    decimal MatchScore,
    string Status,
    bool IsHire,
    DateTimeOffset AppliedAt,
    DateTimeOffset? DecidedAt)
{
    /// <summary>Projects a domain <see cref="RecruitmentApplication"/> into a transport DTO.</summary>
    public static ApplicationDto FromDomain(RecruitmentApplication a)
    {
        ArgumentNullException.ThrowIfNull(a);
        return new(
            a.Id.Value, a.RequestId.Value, a.TalentId, a.TalentType, a.MatchScore, a.Status, a.IsHire, a.AppliedAt, a.DecidedAt);
    }
}
