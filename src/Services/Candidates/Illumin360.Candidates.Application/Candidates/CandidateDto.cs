using Illumin360.Candidates.Domain;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>Read model returned to callers for a candidate.</summary>
/// <param name="Id">Candidate id.</param>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name.</param>
/// <param name="City">City.</param>
/// <param name="Nationality">Nationality.</param>
/// <param name="Availability">Availability status.</param>
/// <param name="PublicHeadline">Optional public headline.</param>
public sealed record CandidateDto(
    Guid Id,
    string FirstName,
    string LastName,
    string City,
    string Nationality,
    string Availability,
    string? PublicHeadline)
{
    /// <summary>Projects a domain <see cref="Candidate"/> into a transport DTO.</summary>
    public static CandidateDto FromDomain(Candidate c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new(
            c.Id.Value, c.FirstName, c.LastName, c.City, c.Nationality, c.Availability.ToString(), c.PublicHeadline);
    }
}
