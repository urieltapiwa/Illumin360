using Illumin360.SharedKernel;

namespace Illumin360.Professionals.Domain;

/// <summary>
/// Aggregate root for a professional on the Illumin360 talent marketplace: their career profile,
/// visibility metrics and match/application counters. Matches, pipeline, in-demand roles, skills and
/// activity are modelled as related read entities in the same bounded context (queried by
/// <see cref="ProfessionalId"/>).
/// </summary>
public sealed class Professional : Entity<ProfessionalId>
{
    private Professional(ProfessionalId id)
        : base(id)
    {
    }

    private Professional(
        ProfessionalId id,
        string firstName,
        string lastName,
        string role,
        string city,
        string nationality,
        string availability,
        string headline)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        City = city;
        Nationality = nationality;
        Availability = availability;
        Headline = headline;
        ViewsTrend = [];
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Given name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Family name.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Current or headline role (e.g. "Software Developer").</summary>
    public string Role { get; private set; } = string.Empty;

    /// <summary>Home city.</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>Nationality.</summary>
    public string Nationality { get; private set; } = string.Empty;

    /// <summary>Availability label (e.g. "Open to opportunities").</summary>
    public string Availability { get; private set; } = string.Empty;

    /// <summary>Public headline / tagline.</summary>
    public string Headline { get; private set; } = string.Empty;

    /// <summary>Profile-strength score (0–100).</summary>
    public int ProfileStrength { get; private set; }

    /// <summary>Ranking percentile among peers (lower is better; e.g. top 12%).</summary>
    public int Percentile { get; private set; }

    /// <summary>Membership start year label.</summary>
    public string MemberSince { get; private set; } = string.Empty;

    /// <summary>Total profile views.</summary>
    public int ProfileViews { get; private set; }

    /// <summary>Percentage change in profile views over the trend window.</summary>
    public int ViewsDelta { get; private set; }

    /// <summary>Open match opportunities.</summary>
    public int MatchOpportunities { get; private set; }

    /// <summary>Percentage change in match opportunities.</summary>
    public int MatchDelta { get; private set; }

    /// <summary>Active applications in flight.</summary>
    public int ActiveApplications { get; private set; }

    /// <summary>Employer response rate (percentage).</summary>
    public int ResponseRate { get; private set; }

    /// <summary>Average match score across opportunities (percentage).</summary>
    public int AvgMatch { get; private set; }

    /// <summary>Interviews secured.</summary>
    public int Interviews { get; private set; }

    /// <summary>Profile-view counts per period, oldest first (drives the sparkline).</summary>
    public IReadOnlyList<int> ViewsTrend { get; private set; } = [];

    /// <summary>Benchmark role for the salary comparison.</summary>
    public string SalaryRole { get; private set; } = string.Empty;

    /// <summary>25th-percentile market salary (NAD).</summary>
    public int SalaryP25 { get; private set; }

    /// <summary>Median market salary (NAD).</summary>
    public int SalaryMedian { get; private set; }

    /// <summary>75th-percentile market salary (NAD).</summary>
    public int SalaryP75 { get; private set; }

    /// <summary>This professional's current salary for comparison (NAD).</summary>
    public int SalaryYou { get; private set; }

    /// <summary>When the professional record was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The professional's full display name.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>Updates the availability label (ignored if blank).</summary>
    /// <param name="availability">New availability label.</param>
    public void SetAvailability(string availability)
    {
        if (!string.IsNullOrWhiteSpace(availability))
        {
            Availability = availability.Trim();
        }
    }

    /// <summary>Records that the professional submitted an application (bumps the active count).</summary>
    public void RecordApplication() => ActiveApplications += 1;

    /// <summary>Registers a new professional. Metrics start at zero and accrue with platform use.</summary>
    /// <param name="firstName">Given name.</param>
    /// <param name="lastName">Family name.</param>
    /// <param name="role">Headline role.</param>
    /// <param name="city">Home city.</param>
    /// <param name="nationality">Nationality.</param>
    /// <param name="availability">Availability label.</param>
    /// <param name="headline">Public headline.</param>
    /// <returns>A successful <see cref="Result{T}"/> with the professional, or a validation error.</returns>
    public static Result<Professional> Register(
        string firstName,
        string lastName,
        string role,
        string city,
        string nationality,
        string availability,
        string headline)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Error.Validation("professional.first_name_required", "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return Error.Validation("professional.last_name_required", "Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return Error.Validation("professional.role_required", "Role is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return Error.Validation("professional.city_required", "City is required.");
        }

        var professional = new Professional(
            ProfessionalId.New(),
            firstName.Trim(),
            lastName.Trim(),
            role.Trim(),
            city.Trim(),
            nationality?.Trim() ?? string.Empty,
            availability?.Trim() ?? string.Empty,
            headline?.Trim() ?? string.Empty);

        professional.Raise(new ProfessionalRegistered(professional.Id, professional.FullName, professional.CreatedAt));
        return professional;
    }

    /// <summary>
    /// Rehydrates a fully-specified professional for demo seeding / data import. Unlike
    /// <see cref="Register"/> this sets metrics directly and raises no domain event — it represents
    /// already-existing state, not a new registration.
    /// </summary>
    /// <param name="id">Identity.</param>
    /// <param name="firstName">Given name.</param>
    /// <param name="lastName">Family name.</param>
    /// <param name="role">Headline role.</param>
    /// <param name="city">Home city.</param>
    /// <param name="nationality">Nationality.</param>
    /// <param name="availability">Availability label.</param>
    /// <param name="headline">Public headline.</param>
    /// <param name="profileStrength">Profile-strength score.</param>
    /// <param name="percentile">Ranking percentile.</param>
    /// <param name="memberSince">Membership start year.</param>
    /// <param name="profileViews">Total profile views.</param>
    /// <param name="viewsDelta">Percentage change in views.</param>
    /// <param name="matchOpportunities">Open match opportunities.</param>
    /// <param name="matchDelta">Percentage change in matches.</param>
    /// <param name="activeApplications">Active applications.</param>
    /// <param name="responseRate">Response rate.</param>
    /// <param name="avgMatch">Average match score.</param>
    /// <param name="interviews">Interviews secured.</param>
    /// <param name="viewsTrend">Profile-view trend, oldest first.</param>
    /// <param name="salaryRole">Salary benchmark role.</param>
    /// <param name="salaryP25">25th-percentile salary.</param>
    /// <param name="salaryMedian">Median salary.</param>
    /// <param name="salaryP75">75th-percentile salary.</param>
    /// <param name="salaryYou">This professional's salary.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The hydrated professional.</returns>
    public static Professional Seed(
        Guid id,
        string firstName,
        string lastName,
        string role,
        string city,
        string nationality,
        string availability,
        string headline,
        int profileStrength,
        int percentile,
        string memberSince,
        int profileViews,
        int viewsDelta,
        int matchOpportunities,
        int matchDelta,
        int activeApplications,
        int responseRate,
        int avgMatch,
        int interviews,
        IReadOnlyList<int> viewsTrend,
        string salaryRole,
        int salaryP25,
        int salaryMedian,
        int salaryP75,
        int salaryYou,
        DateTimeOffset createdAt)
        => new(new ProfessionalId(id))
        {
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            City = city,
            Nationality = nationality,
            Availability = availability,
            Headline = headline,
            ProfileStrength = profileStrength,
            Percentile = percentile,
            MemberSince = memberSince,
            ProfileViews = profileViews,
            ViewsDelta = viewsDelta,
            MatchOpportunities = matchOpportunities,
            MatchDelta = matchDelta,
            ActiveApplications = activeApplications,
            ResponseRate = responseRate,
            AvgMatch = avgMatch,
            Interviews = interviews,
            ViewsTrend = viewsTrend,
            SalaryRole = salaryRole,
            SalaryP25 = salaryP25,
            SalaryMedian = salaryMedian,
            SalaryP75 = salaryP75,
            SalaryYou = salaryYou,
            CreatedAt = createdAt,
        };
}

/// <summary>Raised when a new <see cref="Professional"/> is registered.</summary>
/// <param name="ProfessionalId">The new professional's identity.</param>
/// <param name="FullName">The professional's full name.</param>
/// <param name="OccurredOn">When registration occurred (UTC).</param>
public sealed record ProfessionalRegistered(ProfessionalId ProfessionalId, string FullName, DateTimeOffset OccurredOn) : IDomainEvent;
