namespace Illumin360.Candidates.Application.Candidates;

/// <summary>
/// Blind-screening redaction: replaces identity/demographic fields on a candidate projection with a
/// stable anonymous handle so reviewers assess on job-relevant attributes (city, availability, headline)
/// rather than name or nationality. Deterministic — the same candidate always gets the same handle, so it
/// can still be referred to during a review. Pure; the candidate id is retained so recruiters can still
/// act (add to a pool, open notes) without seeing the name.
/// </summary>
public static class BlindRedactor
{
    /// <summary>A stable anonymous label for a candidate (e.g. "Candidate 7F3A").</summary>
    /// <param name="id">The candidate id.</param>
    /// <returns>The anonymous handle.</returns>
    public static string Label(Guid id) => "Candidate " + id.ToString("N", System.Globalization.CultureInfo.InvariantCulture)[..4].ToUpperInvariant();

    /// <summary>Redacts a candidate projection: anonymises the name and hides nationality.</summary>
    /// <param name="candidate">The candidate DTO.</param>
    /// <returns>A redacted copy (city, availability, headline preserved).</returns>
    public static CandidateDto Redact(CandidateDto candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return candidate with
        {
            FirstName = Label(candidate.Id),
            LastName = string.Empty,
            Nationality = "—",
        };
    }
}
