using Illumin360.Recruitment.Domain;

namespace Illumin360.Recruitment.Application.Recruitment;

/// <summary>Read model returned to callers for a recruitment request.</summary>
/// <param name="Id">Request id.</param>
/// <param name="CompanyId">Hiring company id.</param>
/// <param name="Title">Role title.</param>
/// <param name="City">City.</param>
/// <param name="Positions">Number of positions.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="CreatedAt">When posted (UTC).</param>
/// <param name="FilledAt">When filled (UTC), if applicable.</param>
public sealed record RecruitmentRequestDto(
    Guid Id,
    Guid CompanyId,
    string Title,
    string City,
    int Positions,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FilledAt)
{
    /// <summary>Projects a domain <see cref="RecruitmentRequest"/> into a transport DTO.</summary>
    public static RecruitmentRequestDto FromDomain(RecruitmentRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);
        return new(r.Id.Value, r.CompanyId, r.Title, r.City, r.Positions, r.Status.ToString(), r.CreatedAt, r.FilledAt);
    }
}
