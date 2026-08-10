using Illumin360.SharedKernel;

namespace Illumin360.Recruitment.Domain;

/// <summary>Employment type for a requisition.</summary>
public enum EmploymentType
{
    /// <summary>Full-time role.</summary>
    FullTime,

    /// <summary>Part-time role.</summary>
    PartTime,

    /// <summary>Fixed-term contract.</summary>
    Contract,

    /// <summary>Internship.</summary>
    Internship,

    /// <summary>Temporary / seasonal.</summary>
    Temporary,
}

/// <summary>Parsing helpers for <see cref="EmploymentType"/>.</summary>
public static class EmploymentTypes
{
    /// <summary>Parses an employment-type name case-insensitively.</summary>
    /// <param name="value">The name (e.g. <c>fulltime</c>).</param>
    /// <param name="type">The parsed type when successful.</param>
    /// <returns>True if recognised.</returns>
    public static bool TryParse(string? value, out EmploymentType type)
        => Enum.TryParse(value, ignoreCase: true, out type) && Enum.IsDefined(type);

    /// <summary>The canonical lower-case wire name (e.g. <c>fulltime</c>).</summary>
    /// <param name="type">The type.</param>
    /// <returns>The lower-case name.</returns>
    public static string ToWire(this EmploymentType type) => type.ToString().ToLowerInvariant();
}

/// <summary>
/// Service-owned enrichment for a (externally-seeded) recruitment request: salary range, employment type
/// and remote flag. Keyed 1:1 by request id. Tags live in <see cref="RequisitionTag"/>.
/// </summary>
public sealed class RequisitionDetail : Entity<Guid>
{
    private RequisitionDetail(Guid id)
        : base(id)
    {
    }

    /// <summary>The requisition this enriches.</summary>
    public Guid RequestId { get; private init; }

    /// <summary>Lower salary bound (NAD), if set.</summary>
    public int? SalaryMin { get; private set; }

    /// <summary>Upper salary bound (NAD), if set.</summary>
    public int? SalaryMax { get; private set; }

    /// <summary>Currency code.</summary>
    public string Currency { get; private set; } = "NAD";

    /// <summary>Employment type.</summary>
    public EmploymentType EmploymentType { get; private set; }

    /// <summary>Whether the role is remote.</summary>
    public bool Remote { get; private set; }

    /// <summary>When the detail was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates enrichment for a requisition.</summary>
    /// <param name="requestId">The requisition (required).</param>
    /// <param name="salaryMin">Lower salary bound (≥ 0), if any.</param>
    /// <param name="salaryMax">Upper salary bound (≥ min), if any.</param>
    /// <param name="currency">Currency code (defaults to NAD).</param>
    /// <param name="employmentType">Employment-type name.</param>
    /// <param name="remote">Whether remote.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The detail, or a validation error.</returns>
    public static Result<RequisitionDetail> Create(Guid requestId, int? salaryMin, int? salaryMax, string? currency, string? employmentType, bool remote, DateTimeOffset createdAt)
    {
        if (requestId == Guid.Empty)
        {
            return Error.Validation("requisition.request_required", "A request id is required.");
        }

        var validation = Validate(salaryMin, salaryMax, employmentType, out var type);
        if (validation is not null)
        {
            return validation;
        }

        return new RequisitionDetail(Guid.NewGuid())
        {
            RequestId = requestId,
            SalaryMin = salaryMin,
            SalaryMax = salaryMax,
            Currency = string.IsNullOrWhiteSpace(currency) ? "NAD" : currency.Trim().ToUpperInvariant(),
            EmploymentType = type,
            Remote = remote,
            CreatedAt = createdAt,
        };
    }

    /// <summary>Rehydrates a detail for import/seeding.</summary>
    /// <param name="id">Identity.</param>
    /// <param name="requestId">Request id.</param>
    /// <param name="salaryMin">Lower salary bound.</param>
    /// <param name="salaryMax">Upper salary bound.</param>
    /// <param name="currency">Currency.</param>
    /// <param name="employmentType">Employment type.</param>
    /// <param name="remote">Remote flag.</param>
    /// <param name="createdAt">Creation timestamp (UTC).</param>
    /// <returns>The hydrated detail.</returns>
    public static RequisitionDetail Seed(Guid id, Guid requestId, int? salaryMin, int? salaryMax, string currency, EmploymentType employmentType, bool remote, DateTimeOffset createdAt)
        => new(id)
        {
            RequestId = requestId,
            SalaryMin = salaryMin,
            SalaryMax = salaryMax,
            Currency = currency,
            EmploymentType = employmentType,
            Remote = remote,
            CreatedAt = createdAt,
        };

    /// <summary>Updates the enrichment fields.</summary>
    /// <param name="salaryMin">Lower salary bound (≥ 0), if any.</param>
    /// <param name="salaryMax">Upper salary bound (≥ min), if any.</param>
    /// <param name="currency">Currency code (defaults to NAD).</param>
    /// <param name="employmentType">Employment-type name.</param>
    /// <param name="remote">Whether remote.</param>
    /// <returns>Success, or a validation error.</returns>
    public Result<RequisitionDetail> Update(int? salaryMin, int? salaryMax, string? currency, string? employmentType, bool remote)
    {
        var validation = Validate(salaryMin, salaryMax, employmentType, out var type);
        if (validation is not null)
        {
            return validation;
        }

        SalaryMin = salaryMin;
        SalaryMax = salaryMax;
        Currency = string.IsNullOrWhiteSpace(currency) ? "NAD" : currency.Trim().ToUpperInvariant();
        EmploymentType = type;
        Remote = remote;
        return this;
    }

    private static Error? Validate(int? salaryMin, int? salaryMax, string? employmentType, out EmploymentType type)
    {
        type = EmploymentType.FullTime;

        if (salaryMin is < 0 || salaryMax is < 0)
        {
            return Error.Validation("requisition.salary_negative", "Salary values cannot be negative.");
        }

        if (salaryMin is { } lo && salaryMax is { } hi && lo > hi)
        {
            return Error.Validation("requisition.salary_range", "The minimum salary cannot exceed the maximum.");
        }

        if (!string.IsNullOrWhiteSpace(employmentType) && !EmploymentTypes.TryParse(employmentType, out type))
        {
            return Error.Validation("requisition.employment_type_invalid", "Employment type must be one of fulltime, parttime, contract, internship or temporary.");
        }

        return null;
    }
}

/// <summary>A category tag on a requisition (unique per request, normalised).</summary>
public sealed class RequisitionTag : Entity<Guid>
{
    private RequisitionTag(Guid id)
        : base(id)
    {
    }

    /// <summary>The tagged requisition.</summary>
    public Guid RequestId { get; private init; }

    /// <summary>The tag label (lower-cased).</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>Creates a requisition tag, normalising the label.</summary>
    /// <param name="requestId">The requisition (required).</param>
    /// <param name="label">The tag label (required, ≤ 40 chars).</param>
    /// <returns>The tag, or a validation error.</returns>
    public static Result<RequisitionTag> Create(Guid requestId, string label)
    {
        if (requestId == Guid.Empty)
        {
            return Error.Validation("requisition.request_required", "A request id is required.");
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return Error.Validation("tag.label_required", "A tag label is required.");
        }

        var normalized = label.Trim().ToLowerInvariant();
        if (normalized.Length > 40)
        {
            return Error.Validation("tag.label_too_long", "A tag must be 40 characters or fewer.");
        }

        return new RequisitionTag(Guid.NewGuid()) { RequestId = requestId, Label = normalized };
    }
}
