using Illumin360.SharedKernel;

namespace Illumin360.Candidates.Domain;

/// <summary>
/// A candidate in the talent pool (client-facing "Professional" or "Student"; technical
/// identifier <c>job_seeker</c>). Aggregate root for the Candidates bounded context.
/// </summary>
public sealed class Candidate : Entity<CandidateId>
{
    // EF Core materialisation constructor.
    private Candidate(CandidateId id) : base(id) { }

    private Candidate(
        CandidateId id,
        string firstName,
        string lastName,
        string city,
        string nationality,
        AvailabilityStatus availability,
        string? publicHeadline) : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        City = city;
        Nationality = nationality;
        Availability = availability;
        PublicHeadline = publicHeadline;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Candidate's given name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Candidate's family name.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>City the candidate is based in.</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>Candidate's nationality.</summary>
    public string Nationality { get; private set; } = string.Empty;

    /// <summary>Current availability for opportunities.</summary>
    public AvailabilityStatus Availability { get; private set; }

    /// <summary>Optional public profile headline (max 150 chars per schema).</summary>
    public string? PublicHeadline { get; private set; }

    /// <summary>When the candidate record was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Registers a new candidate, enforcing domain invariants. Returns a validation
    /// <see cref="Error"/> instead of throwing for expected bad input (charter Part 18).
    /// </summary>
    /// <param name="firstName">Given name (required).</param>
    /// <param name="lastName">Family name (required).</param>
    /// <param name="city">City (required).</param>
    /// <param name="nationality">Nationality (required).</param>
    /// <param name="availability">Initial availability status.</param>
    /// <param name="publicHeadline">Optional headline (≤ 150 chars).</param>
    /// <returns>A registered <see cref="Candidate"/> or a validation error.</returns>
    public static Result<Candidate> Register(
        string firstName,
        string lastName,
        string city,
        string nationality,
        AvailabilityStatus availability = AvailabilityStatus.ActivelyLooking,
        string? publicHeadline = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Error.Validation("candidate.first_name_required", "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return Error.Validation("candidate.last_name_required", "Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return Error.Validation("candidate.city_required", "City is required.");
        }

        if (string.IsNullOrWhiteSpace(nationality))
        {
            return Error.Validation("candidate.nationality_required", "Nationality is required.");
        }

        if (publicHeadline is { Length: > 150 })
        {
            return Error.Validation("candidate.headline_too_long", "Public headline must be 150 characters or fewer.");
        }

        var candidate = new Candidate(
            CandidateId.New(), firstName.Trim(), lastName.Trim(), city.Trim(), nationality.Trim(), availability, publicHeadline?.Trim());

        candidate.Raise(new CandidateRegistered(candidate.Id, DateTimeOffset.UtcNow));
        return candidate;
    }
}

/// <summary>Raised when a new candidate is registered into the talent pool.</summary>
/// <param name="CandidateId">The new candidate's id.</param>
/// <param name="OccurredOn">When registration occurred (UTC).</param>
public sealed record CandidateRegistered(CandidateId CandidateId, DateTimeOffset OccurredOn) : IDomainEvent;
