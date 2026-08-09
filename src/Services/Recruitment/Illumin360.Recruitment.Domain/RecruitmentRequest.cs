using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>
/// A hiring brief posted by a business. Aggregate root of the Recruitment bounded context
/// (charter Part 5). Candidates are matched and apply against a request.
/// </summary>
public sealed class RecruitmentRequest : Entity<RequestId>
{
    // EF Core materialisation constructor.
    private RecruitmentRequest(RequestId id) : base(id) { }

    private RecruitmentRequest(RequestId id, Guid companyId, string title, string city, int positions) : base(id)
    {
        CompanyId = companyId;
        Title = title;
        City = city;
        Positions = positions;
        Status = RequestStatus.Open;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>The hiring company's id.</summary>
    public Guid CompanyId { get; private set; }

    /// <summary>The role title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>City the role is based in.</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>Number of positions to fill.</summary>
    public int Positions { get; private set; }

    /// <summary>Current lifecycle status.</summary>
    public RequestStatus Status { get; private set; }

    /// <summary>When the request was posted (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When the request was filled (UTC), if applicable.</summary>
    public DateTimeOffset? FilledAt { get; private set; }

    /// <summary>
    /// Posts a new recruitment request, enforcing domain invariants. Returns a validation
    /// <see cref="Error"/> instead of throwing for expected bad input (charter Part 18).
    /// </summary>
    /// <param name="companyId">The hiring company's id (required).</param>
    /// <param name="title">Role title (required).</param>
    /// <param name="city">City (required).</param>
    /// <param name="positions">Number of positions (at least 1).</param>
    /// <returns>A posted <see cref="RecruitmentRequest"/> or a validation error.</returns>
    public static Result<RecruitmentRequest> Post(Guid companyId, string title, string city, int positions)
    {
        if (companyId == Guid.Empty)
        {
            return Error.Validation("request.company_required", "A company is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Error.Validation("request.title_required", "A role title is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return Error.Validation("request.city_required", "A city is required.");
        }

        if (positions < 1)
        {
            return Error.Validation("request.positions_invalid", "At least one position is required.");
        }

        var request = new RecruitmentRequest(RequestId.New(), companyId, title.Trim(), city.Trim(), positions);
        request.Raise(new RequestPosted(request.Id, companyId, DateTimeOffset.UtcNow));
        return request;
    }

    /// <summary>Marks the request as filled.</summary>
    public void MarkFilled()
    {
        Status = RequestStatus.Filled;
        FilledAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Raised when a new recruitment request is posted into the platform.</summary>
/// <param name="RequestId">The new request's id.</param>
/// <param name="CompanyId">The hiring company's id.</param>
/// <param name="OccurredOn">When the request was posted (UTC).</param>
public sealed record RequestPosted(RequestId RequestId, Guid CompanyId, DateTimeOffset OccurredOn) : IDomainEvent;
